using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
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
                        Action createAction = () => AscieReadModel.Create(readArchive, MessageDigestAlgorithm.SHA256);
                        createAction.Should().Throw<ArgumentException>().And.ParamName.Should().Be("zipArchive");
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
                        Action createAction = () => AscieReadModel.Create(readArchive, MessageDigestAlgorithm.SHA512);
                        createAction.Should().Throw<ArgumentException>().And.ParamName.Should().Be("zipArchive");
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
                        var asiceArchive = AscieReadModel.Create(readArchive, MessageDigestAlgorithm.SHA256);
                        asiceArchive.Should().NotBeNull();
                        asiceArchive.Entries.Count().Should().Be(0);
                        asiceArchive.CadesManifest.Should().BeNull();
                    }
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
                using (var asiceBuilder = AsiceBuilder.Create(outputStream, MessageDigestAlgorithm.SHA256, signingCertificates))
                {
                    asiceBuilder.AddFile(textFileStream, contentFile, MimeType.ForString("text/plain"));
                    asiceBuilder.Build().Should().NotBeNull();
                }

                using (var readStream = new MemoryStream(outputStream.ToArray()))
                using (var zip = new ZipArchive(readStream))
                {
                    var asicePackage = AscieReadModel.Create(zip, MessageDigestAlgorithm.SHA256);
                    var entries = asicePackage.Entries;
                    entries.Count().Should().Be(1);
                    var cadesManifest = asicePackage.CadesManifest;
                    cadesManifest.Should().NotBeNull();

                    cadesManifest.Digests.Count.Should().Be(1);
                    asicePackage.DigestVerifier.Should().NotBeNull();
                    cadesManifest.SignatureFileName.Should().NotBeNull();

                    var firstEntry = entries.First();
                    using (var entryStream = firstEntry.OpenStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        entryStream.CopyTo(memoryStream);
                        Encoding.UTF8.GetString(memoryStream.ToArray()).Should().Be(content);
                    }

                    var verificationResult = asicePackage.DigestVerifier.Verification();
                    verificationResult.Should().NotBeNull();
                    verificationResult.AllValid.Should().BeTrue();
                }
            }
        }
    }
}