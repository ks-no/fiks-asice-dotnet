using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class AsiceReadModelTest
    {
        [Fact(DisplayName = "Read empty Zip")]
        public void ReadEmptyAsicE()
        {
            using (var zipOutStream = new MemoryStream())
            {
                var createdArchive = new ZipArchive(zipOutStream, ZipArchiveMode.Create);
                createdArchive.Dispose();
                using (var zipInStream = new MemoryStream(zipOutStream.ToArray()))
                {
                    using (var readArchive = new ZipArchive(zipInStream, ZipArchiveMode.Read))
                    {
                        Action createAction = () => new AsiceReadModel(readArchive);
                        createAction.ShouldThrow<ArgumentException>().ParamName.ShouldBe("zipArchive");
                    }
                }
            }
        }

        [Fact(DisplayName = "Read non-empty Zip that is missing the required first entry")]
        public void ReadAsicEWithoutRequiredFirstEntry()
        {
            using (var zipOutStream = new MemoryStream())
            {
                using (var createdArchive = new ZipArchive(zipOutStream, ZipArchiveMode.Create))
                {
                    var newEntry = createdArchive.CreateEntry("file.txt");
                    using (var entryStream = newEntry.Open())
                    {
                        entryStream.Write(Encoding.UTF8.GetBytes("Lorem Ipsum"));
                    }
                }

                using (var zipInStream = new MemoryStream(zipOutStream.ToArray()))
                {
                    using (var readArchive = new ZipArchive(zipInStream, ZipArchiveMode.Read))
                    {
                        Action createAction = () => new AsiceReadModel(readArchive);
                        createAction.ShouldThrow<ArgumentException>().ParamName.ShouldBe("zipArchive");
                    }
                }
            }
        }

        [Fact(DisplayName = "Read non-empty Zip that is missing manifest")]
        public void ReadAsicEWithoutManifestAndSignature()
        {
            using (var zipOutStream = new MemoryStream())
            {
                using (var createdArchive = new ZipArchive(zipOutStream, ZipArchiveMode.Create))
                {
                    var newEntry = createdArchive.CreateEntry(AsiceConstants.FileNameMimeType);
                    using (var entryStream = newEntry.Open())
                    {
                        entryStream.Write(Encoding.UTF8.GetBytes(AsiceConstants.ContentTypeASiCe));
                    }
                }

                using (var zipInStream = new MemoryStream(zipOutStream.ToArray()))
                {
                    using (var readArchive = new ZipArchive(zipInStream, ZipArchiveMode.Read))
                    {
                        var asiceArchive = new AsiceReadModel(readArchive);
                        asiceArchive.ShouldNotBeNull();
                        asiceArchive.Entries.Count().ShouldBe(0);
                        asiceArchive.CadesManifest.ShouldBeNull();
                    }
                }
            }
        }

        [Fact(DisplayName = "Read ASiC-E package from resource")]
        public void ReadAsiceResource()
        {
            using (var asicStream = TestDataUtil.ReadValidAsiceCadesFromResource())
            {
                using (var zip = new ZipArchive(asicStream, ZipArchiveMode.Read))
                using (var asice = new AsiceReadModel(zip))
                {
                    asice.CadesManifest.ShouldNotBeNull();
                    asice.Signatures.ShouldNotBeNull();
                    foreach (var asiceReadEntry in asice.Entries)
                    {
                        using (var entryStream = asiceReadEntry.OpenStream())
                        using (var bufferStream = new MemoryStream())
                        {
                            entryStream.CopyTo(bufferStream);
                            bufferStream.Position.ShouldBeGreaterThan(0);
                        }
                    }

                    asice.DigestVerifier.Verification().AllValid.ShouldBeTrue();
                }
            }
        }

        [Fact(DisplayName = "Read valid AsicE archive")]
        public void ReadAsiceWithCadesManifestAndSignature()
        {
            var signingCertificates = TestdataLoader.ReadCertificatesForTest();
            const string contentFile = "filename.txt";
            const string content = "Lorem ipsum";

            using (var outputStream = new MemoryStream())
            {
                using (var textFileStream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                using (var asiceBuilder = AsiceBuilder.Create(
                    outputStream,
                    MessageDigestAlgorithm.SHA256,
                    ManifestSpec.Cades,
                    signingCertificates))
                {
                    asiceBuilder.AddFile(textFileStream, contentFile, MimeType.ForString("text/plain"));
                    asiceBuilder.Build().ShouldNotBeNull();
                }

                using (var readStream = new MemoryStream(outputStream.ToArray()))
                using (var zip = new ZipArchive(readStream))
                {
                    var asicePackage = new AsiceReadModel(zip);
                    var entries = asicePackage.Entries;
                    entries.Count().ShouldBe(1);
                    var cadesManifest = asicePackage.CadesManifest;
                    cadesManifest.ShouldNotBeNull();

                    cadesManifest.Digests.Count.ShouldBe(1);
                    asicePackage.DigestVerifier.ShouldNotBeNull();
                    cadesManifest.SignatureFileName.ShouldNotBeNull();
                    asicePackage.Signatures.ShouldNotBeNull();
                    asicePackage.Signatures.Containers.Count().ShouldBe(1);

                    var firstEntry = entries.First();
                    using (var entryStream = firstEntry.OpenStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        entryStream.CopyTo(memoryStream);
                        Encoding.UTF8.GetString(memoryStream.ToArray()).ShouldBe(content);
                    }

                    var verificationResult = asicePackage.DigestVerifier.Verification();
                    verificationResult.ShouldNotBeNull();
                    verificationResult.AllValid.ShouldBeTrue();
                }
            }
        }
    }
}