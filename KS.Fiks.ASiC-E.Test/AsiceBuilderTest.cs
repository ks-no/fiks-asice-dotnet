using System;
using System.IO;
using System.IO.Compression;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Moq;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test;

public class AsiceBuilderTest
{
    [Fact(DisplayName = "Try to create builder using non-writable stream")]
    public void TestNotWritableStream()
    {
        var zipStream = new Mock<Stream>();
        var certificate = new Mock<ICertificateHolder>();
        Action createFunction = () =>
            AsiceBuilder.Create(zipStream.Object, MessageDigestAlgorithm.SHA512, certificate.Object);
        createFunction.ShouldThrow<ArgumentException>();
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
                asiceBuilder.ShouldNotBeNull();

                asiceBuilder.AddFile(fileStream).ShouldNotBeNull().ShouldBeOfType<AsiceBuilder>();

                var asiceArchive = asiceBuilder.Build();
                asiceArchive.ShouldNotBeNull();
            }

            zippedBytes = zipStream.ToArray();
        }

        zippedBytes.Length.ShouldBeGreaterThan(0);

        using (var zipStream = new MemoryStream(zippedBytes))
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            zipArchive.Entries.Count.ShouldBe(4);
        }
    }

    [Theory]
    [InlineData("root/small.pdf")]
    [InlineData("root/someFolder/small.pdf")]
    [InlineData("root/someFolder/newName.pdf")]
    public void TestAddFileWithPathAddsFileInFolder(string fullPath)
    {
        byte[] zippedBytes;

        var signingCertificates = TestdataLoader.ReadCertificatesForTest();
        using (var zipStream = new MemoryStream())
        using (var fileStream = File.OpenRead("small.pdf"))
        {
            using (var asiceBuilder =
                AsiceBuilder.Create(zipStream, MessageDigestAlgorithm.SHA256, signingCertificates))
            {
                asiceBuilder.ShouldNotBeNull();

                asiceBuilder.AddFileWithPath(fileStream, fullPath,  MimeTypeExtractor.ExtractMimeType("small.pdf")).ShouldNotBeNull().ShouldBeOfType<AsiceBuilder>();

                var asiceArchive = asiceBuilder.Build();
                asiceArchive.ShouldNotBeNull();
            }

            zippedBytes = zipStream.ToArray();
        }

        zippedBytes.Length.ShouldBeGreaterThan(0);

        using (var zipStream = new MemoryStream(zippedBytes))
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            zipArchive.Entries.Count.ShouldBe(4);
            zipArchive.Entries.ShouldContain(e => e.FullName == fullPath);
        }
    }
}