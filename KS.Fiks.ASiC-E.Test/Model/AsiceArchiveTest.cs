using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FluentAssertions;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using NLog;
using Xunit;
using static MimeMapping.MimeUtility;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class AsiceArchiveTest : IClassFixture<LogFixture>
    {
        private const string FileNameTestPdf = "small.pdf";
        private readonly Logger log;
        private LogFixture logFixture;

        public AsiceArchiveTest(LogFixture logFixture)
        {
            this.logFixture = logFixture;
            this.log = LogFixture.GetLog<AsiceArchiveTest>();
        }

        [Fact(DisplayName = "Create ASiC-E package with public and private keys for signing")]
        public void CreateArchiveWithPublicAndPrivateKeysForSigning()
        {
            var certHolder = TestdataLoader.ReadCertificatesForTest();
            TestArchive(certHolder);
        }

        [Fact(DisplayName = "Create ASiC-E package with X509Certificate2 for signing")]
        public void CreateArchiveWithX509Certificate2ForSigning()
        {
            var certHolder = TestdataLoader.ReadX509Certificate2ForTest();
            TestArchive(certHolder);
        }

        private void TestArchive(ICertificateHolder certHolder)
        {
            byte[] zippedData;
            using (var zippedOutStream = new MemoryStream())
            {
                using (var archive = AsiceArchive.Create(
                           zippedOutStream,
                           CadesManifestCreator.CreateWithSignatureFile(),
                           certHolder
                       ))
                using (var fileStream = File.OpenRead(FileNameTestPdf))
                {
                    archive.AddEntry(
                        fileStream,
                        new FileRef(FileNameTestPdf, MimeType.ForString(UnknownMimeType)));
                }

                zippedData = zippedOutStream.ToArray();
                zippedData.Should().NotBeNull();
                zippedData.Should().HaveCountGreaterThan(0);
            }

            using (var zipInput = new MemoryStream(zippedData))
            using (var zippedArchive = new ZipArchive(zipInput, ZipArchiveMode.Read))
            {
                zippedArchive.Entries.Should().HaveCount(4);
                zippedArchive.Entries.First(e => e.FullName.Equals(FileNameTestPdf, StringComparison.CurrentCulture)).Should()
                    .NotBeNull();
                zippedArchive.Entries
                    .First(e => e.FullName.Equals(AsiceConstants.CadesManifestFilename, StringComparison.CurrentCulture))
                    .Should().NotBeNull();
                zippedArchive.Entries
                    .First(e => e.FullName.Equals(AsiceConstants.FileNameMimeType, StringComparison.CurrentCulture)).Should()
                    .NotBeNull();

                var mimeTypeEntry = zippedArchive.GetEntry(AsiceConstants.FileNameMimeType);
                using (var entryStream = mimeTypeEntry.Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);
                    Encoding.UTF8.GetString(copyStream.ToArray()).Should().Be(AsiceConstants.ContentTypeASiCe);
                }

                // Verifies that a CADES manifest has been generated
                using (var entryStream = zippedArchive.GetEntry(AsiceConstants.CadesManifestFilename).Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);

                    var manifestXml = Encoding.UTF8.GetString(copyStream.ToArray());
                    manifestXml.Should().NotBeNull();
                    this.log.Info($"Manifest: {manifestXml}");
                }

                var signatureFile = zippedArchive.Entries
                    .First(e => e.FullName.StartsWith("META-INF", StringComparison.CurrentCulture) &&
                                e.FullName.EndsWith(".p7s", StringComparison.CurrentCulture));
                signatureFile.Should().NotBeNull();

                // Verifies the signature file
                using (var entryStream = signatureFile.Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);
                    var signatureContent = copyStream.ToArray();
                    signatureContent.Should().HaveCountGreaterThan(0);
                }
            }

            var tempFileName = Path.GetTempFileName();
            using (var zippedStream = new MemoryStream(zippedData))
            using (var outputFileStream = File.OpenWrite(tempFileName))
            {
                zippedStream.CopyTo(outputFileStream);
            }

            this.log.Info($"Wrote package to '{tempFileName}'");
        }

        [Fact(DisplayName = "Create ASiC-E package and add manifest manually")]
        public void CreateArchiveWithReuseOfZippedOutStream()
        {
            using (var zippedOutStream = new MemoryStream())
            {
                using (var archive = AsiceArchive.Create(
                           zippedOutStream,
                           CadesManifestCreator.CreateWithSignatureFile(),
                           TestdataLoader.ReadCertificatesForTest()))
                using (var fileStream = File.OpenRead(FileNameTestPdf))
                {
                    archive.AddEntry(
                        fileStream,
                        new FileRef(FileNameTestPdf, MimeType.ForString(GetMimeMapping(FileNameTestPdf))));
                }

                var zippedData = zippedOutStream.ToArray();
                zippedData.Should().NotBeNull();
                zippedData.Should().HaveCountGreaterThan(0);

                Assert.True(zippedOutStream.CanSeek);
                zippedOutStream.Seek(0, SeekOrigin.Begin);

                using (var zippedArchive = new ZipArchive(zippedOutStream, ZipArchiveMode.Read, leaveOpen: true))
                {
                    zippedArchive.Entries.Should().HaveCount(4);
                    zippedArchive.Entries
                        .First(e => e.FullName.Equals(FileNameTestPdf, StringComparison.CurrentCulture)).Should()
                        .NotBeNull();
                    zippedArchive.Entries.First(e =>
                            e.FullName.Equals(AsiceConstants.CadesManifestFilename, StringComparison.CurrentCulture))
                        .Should().NotBeNull();
                    zippedArchive.Entries.First(e =>
                            e.FullName.Equals(AsiceConstants.FileNameMimeType, StringComparison.CurrentCulture))
                        .Should()
                        .NotBeNull();

                    var mimeTypeEntry = zippedArchive.GetEntry(AsiceConstants.FileNameMimeType);
                    using (var entryStream = mimeTypeEntry.Open())
                    using (var copyStream = new MemoryStream())
                    {
                        entryStream.CopyTo(copyStream);
                        Encoding.UTF8.GetString(copyStream.ToArray()).Should().Be(AsiceConstants.ContentTypeASiCe);
                    }

                    // Verifies that a CADES manifest has been generated
                    using (var entryStream = zippedArchive.GetEntry(AsiceConstants.CadesManifestFilename).Open())
                    using (var copyStream = new MemoryStream())
                    {
                        entryStream.CopyTo(copyStream);

                        var manifestXml = Encoding.UTF8.GetString(copyStream.ToArray());
                        manifestXml.Should().NotBeNull();
                        this.log.Info($"Manifest: {manifestXml}");
                    }

                    var signatureFile = zippedArchive.Entries
                        .First(e => e.FullName.StartsWith("META-INF", StringComparison.CurrentCulture) &&
                                    e.FullName.EndsWith(".p7s", StringComparison.CurrentCulture));
                    signatureFile.Should().NotBeNull();

                    // Verifies the signature file
                    using (var entryStream = signatureFile.Open())
                    using (var copyStream = new MemoryStream())
                    {
                        entryStream.CopyTo(copyStream);
                        var signatureContent = copyStream.ToArray();
                        signatureContent.Should().HaveCountGreaterThan(0);
                    }
                }

                var tempFileName = Path.GetTempFileName();
                zippedOutStream.Seek(0, SeekOrigin.Begin);
                using (var outputFileStream = File.OpenWrite(tempFileName))
                {
                    zippedOutStream.CopyTo(outputFileStream);
                }

                this.log.Info($"Wrote package to '{tempFileName}'");
            }
        }
    }
}