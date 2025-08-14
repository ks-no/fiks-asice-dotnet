using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using static MimeMapping.MimeUtility;

namespace KS.Fiks.ASiC_E.Test.Model;

public class AsiceArchiveTest
{
    private const string FileNameTestPdf = "small.pdf";

    private readonly ITestOutputHelper _testOutputHelper;

    public AsiceArchiveTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "Create ASiC-E package in CAdES mode with public and private keys for signing")]
    public void CreateArchiveWithPublicAndPrivateKeysForSigningWithCades()
    {
        var certHolder = TestdataLoader.ReadCertificatesForTest();
        TestArchive(certHolder, ManifestSpec.Cades);
    }

    [Fact(DisplayName = "Create ASiC-E package CAdES mode with X509Certificate2 for signing")]
    public void CreateArchiveWithX509Certificate2ForSigningWithCades()
    {
        var certHolder = TestdataLoader.ReadX509Certificate2ForTest();
        TestArchive(certHolder, ManifestSpec.Cades);
    }

    [Fact(DisplayName = "Create ASiC-E package CAdES mode with null ICertificateHolder in")]
    public void CreateArchiveWithoutCertificateHolderWithCadesManifestSpec()
    {
        TestArchive(null, ManifestSpec.Cades);
    }

    [Fact(DisplayName = "Create ASiC-E package XAdES mode with public and private keys for signing")]
    public void CreateArchiveWithPublicAndPrivateKeysForSigningWithXades()
    {
        var certHolder = TestdataLoader.ReadCertificatesForTest();
        TestArchive(certHolder, ManifestSpec.Xades);
    }

    [Fact(DisplayName = "Create ASiC-E package XAdES mode with X509Certificate2 for signing")]
    public void CreateArchiveWithX509Certificate2ForSigningWithXades()
    {
        var certHolder = TestdataLoader.ReadX509Certificate2ForTest();
        TestArchive(certHolder, ManifestSpec.Xades);
    }

    [Fact(DisplayName = "Create ASiC-E package XAdES mode with null ICertificateHolder")]
    public void CreateArchiveWithoutCertificateHolderWithXadesManifestSpec()
    {
        TestArchive(null, ManifestSpec.Xades);
    }

    private void TestArchive(ICertificateHolder certHolder, ManifestSpec spec)
    {
        byte[] zippedData;
        using (var zippedOutStream = new MemoryStream())
        {
            // signatureFileRefCreator and signatureCreator should both be
            // null if and only if certHolder is null:
            ISignatureFileRefCreator signatureFileRefCreator = certHolder == null
                ? null
                : (spec == ManifestSpec.Cades
                    ? new CadesSignature()
                    : new XadesSignature());

            ISignatureCreator signatureCreator = certHolder == null
                ? null
                : (spec == ManifestSpec.Cades
                    ? SignatureCreator.Create(certHolder)
                    : XadesSignatureCreator.Create(certHolder));

            // Perhaps not realistic to use a CAdES manifest with both CAdES
            // and XAdES signatures, but in the absence of a distinct XAdES
            // manifest format in the library, we use this for now:
            var cadesManifestCreator = new CadesManifestCreator();
            using (var archive = new AsiceArchive(
                zippedOutStream,
                cadesManifestCreator,
                signatureFileRefCreator,
                signatureCreator))
            using (var fileStream = File.OpenRead(FileNameTestPdf))
            {
                archive.AddEntry(
                    fileStream,
                    new FileRef(FileNameTestPdf, MimeType.ForString(GetMimeMapping(FileNameTestPdf))));
            }

            zippedData = zippedOutStream.ToArray();
            zippedData.ShouldNotBeNull();
            zippedData.Length.ShouldBeGreaterThan(0);

            using (var zipInput = new MemoryStream(zippedData))
            using (var zippedArchive = new ZipArchive(zipInput, ZipArchiveMode.Read))
            {
                zippedArchive.Entries.Count.ShouldBe(certHolder == null ? 3 : 4);
                zippedArchive.Entries.First(e => e.FullName.Equals(FileNameTestPdf, StringComparison.CurrentCulture))
                    .ShouldNotBeNull();
                zippedArchive.Entries
                    .First(e => e.FullName.Equals(AsiceConstants.CadesManifestFilename,
                        StringComparison.CurrentCulture))
                    .ShouldNotBeNull();
                zippedArchive.Entries
                    .First(e => e.FullName.Equals(AsiceConstants.FileNameMimeType, StringComparison.CurrentCulture))
                    .ShouldNotBeNull();

                var mimeTypeEntry = zippedArchive.GetEntry(AsiceConstants.FileNameMimeType);
                using (var entryStream = mimeTypeEntry.Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);
                    Encoding.UTF8.GetString(copyStream.ToArray()).ShouldBe(AsiceConstants.ContentTypeASiCe);
                }

                // Verifies that a CADES manifest has been generated
                using (var entryStream = zippedArchive.GetEntry(AsiceConstants.CadesManifestFilename).Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);

                    var manifestXml = Encoding.UTF8.GetString(copyStream.ToArray());
                    manifestXml.ShouldNotBeNull();
                    _testOutputHelper.WriteLine($"Manifest: {manifestXml}");
                }

                if (certHolder != null)
                {
                    var signatureSearchTerm = spec == ManifestSpec.Cades
                        ? ".p7s"
                        : "signatures.xml";
                    var signatureFile = zippedArchive.Entries
                        .First(e => e.FullName.StartsWith("META-INF", StringComparison.CurrentCulture) &&
                                    e.FullName.EndsWith(signatureSearchTerm, StringComparison.CurrentCulture));
                    signatureFile.ShouldNotBeNull();

                    // Verifies the signature file
                    using (var entryStream = signatureFile.Open())
                    using (var copyStream = new MemoryStream())
                    {
                        entryStream.CopyTo(copyStream);
                        var signatureContent = copyStream.ToArray();
                        signatureContent.Length.ShouldBeGreaterThan(0);
                    }
                }
            }

            var tempFileName = Path.GetTempFileName();
            using (var zippedStream = new MemoryStream(zippedData))
            using (var outputFileStream = File.OpenWrite(tempFileName))
            {
                zippedStream.CopyTo(outputFileStream);
            }

            _testOutputHelper.WriteLine($"Wrote package to '{tempFileName}'");
        }
    }

    [Fact(DisplayName = "Create ASiC-E package and add manifest manually")]
        public void CreateArchiveWithReuseOfZippedOutStream()
        {
            using var zippedOutStream = new MemoryStream();
            var cadesManifestCreator = new CadesManifestCreator();
            var signatureFileRefCreator = new CadesSignature();
            var certHolder = TestdataLoader.ReadCertificatesForTest();
            var signatureCreator = SignatureCreator.Create(certHolder);
            using (var archive = new AsiceArchive(
                zippedOutStream,
                cadesManifestCreator,
                signatureFileRefCreator,
                signatureCreator))
            using (var fileStream = File.OpenRead(FileNameTestPdf))
            {
                archive.AddEntry(
                    fileStream,
                    new FileRef(FileNameTestPdf, MimeType.ForString(GetMimeMapping(FileNameTestPdf))));
            }

            var zippedData = zippedOutStream.ToArray();
            zippedData.ShouldNotBeNull();
            zippedData.Length.ShouldBeGreaterThan(0);

            Assert.True(zippedOutStream.CanSeek);
            zippedOutStream.Seek(0, SeekOrigin.Begin);

            using (var zippedArchive = new ZipArchive(zippedOutStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                zippedArchive.Entries.Count.ShouldBe(4);
                zippedArchive.Entries
                    .First(e => e.FullName.Equals(FileNameTestPdf, StringComparison.CurrentCulture)).ShouldNotBeNull();
                zippedArchive.Entries.First(e =>
                        e.FullName.Equals(AsiceConstants.CadesManifestFilename, StringComparison.CurrentCulture))
                    .ShouldNotBeNull();
                zippedArchive.Entries.First(e =>
                        e.FullName.Equals(AsiceConstants.FileNameMimeType, StringComparison.CurrentCulture))
                    .ShouldNotBeNull();

                var mimeTypeEntry = zippedArchive.GetEntry(AsiceConstants.FileNameMimeType);
                using (var entryStream = mimeTypeEntry.Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);
                    Encoding.UTF8.GetString(copyStream.ToArray()).ShouldBe(AsiceConstants.ContentTypeASiCe);
                }

                // Verifies that a CADES manifest has been generated
                using (var entryStream = zippedArchive.GetEntry(AsiceConstants.CadesManifestFilename).Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);

                    var manifestXml = Encoding.UTF8.GetString(copyStream.ToArray());
                    manifestXml.ShouldNotBeNull();
                    _testOutputHelper.WriteLine($"Manifest: {manifestXml}");
                }

                var signatureFile = zippedArchive.Entries
                    .First(e => e.FullName.StartsWith("META-INF", StringComparison.CurrentCulture) &&
                                e.FullName.EndsWith(".p7s", StringComparison.CurrentCulture));
                signatureFile.ShouldNotBeNull();

                // Verifies the signature file
                using (var entryStream = signatureFile.Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);
                    var signatureContent = copyStream.ToArray();
                    signatureContent.Length.ShouldBeGreaterThan(0);
                }
            }

            var tempFileName = Path.GetTempFileName();
            zippedOutStream.Seek(0, SeekOrigin.Begin);
            using (var outputFileStream = File.OpenWrite(tempFileName))
            {
                zippedOutStream.CopyTo(outputFileStream);
            }

            _testOutputHelper.WriteLine($"Wrote package to '{tempFileName}'");
        }
    }
