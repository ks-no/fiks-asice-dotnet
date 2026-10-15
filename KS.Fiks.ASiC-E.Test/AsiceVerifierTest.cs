using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;
using KS.Fiks.ASiC_E.Test.Sign;
using Org.BouncyCastle.Cms;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class AsiceVerifierTest
    {
        [Fact(DisplayName = "Verify valid ASiC-E with CADES manifest and signature")]
        public void VerifyValidCades()
        {
            var asiceArchive = TestdataLoader.ReadFromResource("cades-valid.asice");
            IAsiceVerifier asiceVerifier = new AsiceVerifier();
            using (var inputStream = new MemoryStream(asiceArchive))
            {
                var asicManifest = asiceVerifier.Verify(inputStream);
                asicManifest.ShouldNotBeNull();
                asicManifest.certificate.ShouldNotBeNull();
                asicManifest.certificate.Length.ShouldBe(1);
                asicManifest.file.ShouldNotBeNull();
                asicManifest.file.Length.ShouldBe(2);
                asicManifest.rootfile.ShouldBeNull();
            }
        }

        [Fact(DisplayName = "Verify package built by the library, with one signer")]
        public void VerifyBuiltPackage()
        {
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            var package = BuildPackage(certificateHolder, "content");

            SignerCount(package).ShouldBe(1);

            using var inputStream = new MemoryStream(package);
            var asicManifest = new AsiceVerifier().Verify(inputStream);
            asicManifest.certificate.Single().certificate1
                .ShouldBe(certificateHolder.GetPublicCertificate().GetEncoded());
        }

        [Fact(DisplayName = "Verify package signed by 4.0.x, with two signers")]
        public void VerifyLegacyPackage()
        {
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            using var zipStream = new MemoryStream();
            using (var archive = new AsiceArchive(
                       zipStream,
                       new CadesManifestCreator(),
                       new CadesSignature(),
                       new LegacySignatureCreator(certificateHolder)))
            {
                archive.AddEntry(
                    new MemoryStream(Encoding.UTF8.GetBytes("content")),
                    new FileRef("file.txt", MimeType.ForString("text/plain")));
            }

            var package = zipStream.ToArray();
            SignerCount(package).ShouldBe(2);

            using var inputStream = new MemoryStream(package);
            new AsiceVerifier().Verify(inputStream).ShouldNotBeNull();
        }

        [Fact(DisplayName = "Package with replaced file and manifest but original signature is rejected")]
        public void ReplacedManifestWithOriginalSignatureIsRejected()
        {
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            var original = TestDataUtil.ReadEntries(BuildPackage(certificateHolder, "original"));
            var tampered = TestDataUtil.ReadEntries(BuildPackage(certificateHolder, "tampered"));

            // The tampered package has a correct manifest for the replaced file, but carries the original signature
            var signatureIndex = tampered.FindIndex(e => IsSignature(e.Name));
            tampered[signatureIndex] = (tampered[signatureIndex].Name, original.Single(e => IsSignature(e.Name)).Data);

            using var inputStream = new MemoryStream(TestDataUtil.WriteEntries(tampered));
            Action verify = () => new AsiceVerifier().Verify(inputStream);

            verify.ShouldThrow<SignatureVerificationException>();
        }

        [Fact(DisplayName = "Package where the signature file is missing is rejected")]
        public void MissingSignatureFileIsRejected()
        {
            var entries = TestDataUtil.ReadEntries(BuildPackage(TestdataLoader.ReadCertificatesForTest(), "content"));
            entries.RemoveAll(e => IsSignature(e.Name));

            using var inputStream = new MemoryStream(TestDataUtil.WriteEntries(entries));
            Action verify = () => new AsiceVerifier().Verify(inputStream);

            verify.ShouldThrow<SignatureVerificationException>().Message.ShouldContain("is missing");
        }

        [Theory(DisplayName = "Verify package with files in folders")]
        [InlineData("root/small.pdf")]
        [InlineData("root/someFolder/small.pdf")]
        public void VerifyFilesInFolders(string path)
        {
            using var zipStream = new MemoryStream();
            using (var builder = AsiceBuilder.Create(
                       zipStream,
                       MessageDigestAlgorithm.SHA256,
                       ManifestSpec.Cades,
                       TestdataLoader.ReadCertificatesForTest()))
            {
                using var pdf = File.OpenRead("small.pdf");
                builder.AddFileWithPath(pdf, path, MimeType.ForString("application/pdf"));
                builder.AddFile(new MemoryStream(Encoding.UTF8.GetBytes("content")), "file.txt");
            }

            using var inputStream = new MemoryStream(zipStream.ToArray());
            var asicManifest = new AsiceVerifier().Verify(inputStream);

            asicManifest.file.Select(f => f.name).ShouldBe(new[] { path, "file.txt" }, ignoreOrder: true);
        }

        [Fact(DisplayName = "Package where a declared file is missing and another file is duplicated is rejected")]
        public void MissingAndDuplicatedFileIsRejected()
        {
            using var zipStream = new MemoryStream();
            using (var builder = AsiceBuilder.Create(
                       zipStream,
                       MessageDigestAlgorithm.SHA256,
                       ManifestSpec.Cades,
                       TestdataLoader.ReadCertificatesForTest()))
            {
                builder.AddFile(new MemoryStream(Encoding.UTF8.GetBytes("a")), "a.txt");
                builder.AddFile(new MemoryStream(Encoding.UTF8.GetBytes("b")), "b.txt");
            }

            // Replace b.txt with a second copy of a.txt, so that the number of files still matches the manifest
            var entries = TestDataUtil.ReadEntries(zipStream.ToArray());
            var a = entries.Single(e => e.Name == "a.txt");
            entries[entries.FindIndex(e => e.Name == "b.txt")] = a;

            using var inputStream = new MemoryStream(TestDataUtil.WriteEntries(entries));
            Action verify = () => new AsiceVerifier().Verify(inputStream);

            verify.ShouldThrow<ArgumentException>();
        }

        private static byte[] BuildPackage(ICertificateHolder certificateHolder, string content)
        {
            using var zipStream = new MemoryStream();
            using (var builder = AsiceBuilder.Create(
                       zipStream,
                       MessageDigestAlgorithm.SHA256,
                       ManifestSpec.Cades,
                       certificateHolder))
            {
                builder.AddFile(new MemoryStream(Encoding.UTF8.GetBytes(content)), "file.txt");
            }

            return zipStream.ToArray();
        }

        private static bool IsSignature(string name) => name.EndsWith(".p7s", StringComparison.Ordinal);

        private static int SignerCount(byte[] package)
        {
            var entries = TestDataUtil.ReadEntries(package);
            var manifest = entries.Single(e => e.Name == AsiceConstants.CadesManifestFilename).Data;
            var signature = entries.Single(e => IsSignature(e.Name)).Data;
            return new CmsSignedData(new CmsProcessableByteArray(manifest), signature).GetSignerInfos().Count;
        }
    }
}
