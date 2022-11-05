using System;
using System.IO;
using System.IO.Compression;
using FluentAssertions;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Moq;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class AsiceBuilderTest : IClassFixture<LogFixture>
    {
        private readonly LogFixture logFixture;

        public AsiceBuilderTest(LogFixture logFixture)
        {
            this.logFixture = logFixture;
        }

        [Fact(DisplayName = "Try to create builder using non-writable stream")]
        public void TestNotWritableStream()
        {
            var zipStream = new Mock<Stream>();
            var certificate = new Mock<ICertificateHolder>();
            Action createFunction = () =>
                AsiceBuilder.Create(zipStream.Object, MessageDigestAlgorithm.SHA512, certificate.Object);
            createFunction.Should().Throw<ArgumentException>();
            zipStream.VerifyGet(s => s.CanWrite);
            zipStream.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestAddFileStream()
        {
            byte[] zippedBytes;

            var signingCertificates = TestdataLoader.ReadCertificatesForTest();
            using (var zipStream = new MemoryStream())
            using (var fileStream = File.OpenRead("small.pdf"))
            {
                using (var asiceBuilder =
                    AsiceBuilder.Create(zipStream, MessageDigestAlgorithm.SHA256, signingCertificates))
                {
                    asiceBuilder.Should().NotBeNull();

                    asiceBuilder.AddFile(fileStream).Should().NotBeNull().And.BeOfType<AsiceBuilder>();

                    var asiceArchive = asiceBuilder.Build();
                    asiceArchive.Should().NotBeNull();
                }

                zippedBytes = zipStream.ToArray();
            }

            LogFixture.GetLog<AsiceBuilderTest>().Info($"Created zip containing {zippedBytes.Length} bytes");
            zippedBytes.Should().HaveCountGreaterThan(0);

            using (var zipStream = new MemoryStream(zippedBytes))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                zipArchive.Entries.Count.Should().Be(4);
            }
        }

        [Fact(DisplayName = "When a container entry is marked as root file, the container manifest must contain that root file.")]
        public void SetFileAsRoot_ManifestContainsRootFile()
        {
            byte[] archive;

            var signingCertificates = TestdataLoader.ReadCertificatesForTest();
            using (var outStream = new MemoryStream())
            {
                using var fileStream = File.OpenRead("small.pdf");
                using (var asiceBuilder = AsiceBuilder.Create(outStream, MessageDigestAlgorithm.SHA256, signingCertificates))
                {
                    asiceBuilder.AddFile(fileStream, true);
                    asiceBuilder.Build();
                }

                archive = outStream.ToArray();
            }

            using var inStream = new MemoryStream(archive);
            IAsicReader reader = new AsiceReader();
            using var asice = reader.Read(inStream);

            asice.CadesManifest.RootFile.Should().Be("small.pdf");
        }

        [Fact(DisplayName = "When no container entry is marked as root file, the container manifest must not contain any root file.")]
        public void FileNotSetAsRoot_ManifestContainsNoRootFile()
        {
            byte[] archive;

            var signingCertificates = TestdataLoader.ReadCertificatesForTest();
            using (var outStream = new MemoryStream())
            {
                using var fileStream = File.OpenRead("small.pdf");
                using (var asiceBuilder = AsiceBuilder.Create(outStream, MessageDigestAlgorithm.SHA256, signingCertificates))
                {
                    asiceBuilder.AddFile(fileStream);
                    asiceBuilder.Build();
                }

                archive = outStream.ToArray();
            }

            using var inStream = new MemoryStream(archive);
            IAsicReader reader = new AsiceReader();
            using var asice = reader.Read(inStream);

            asice.CadesManifest.RootFile.Should().BeNullOrEmpty();
        }
    }
}