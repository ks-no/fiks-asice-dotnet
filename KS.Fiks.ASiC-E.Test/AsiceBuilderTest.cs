using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            AsiceBuilder.Create(
                zipStream.Object,
                MessageDigestAlgorithm.SHA512,
                ManifestSpec.Cades,
                certificate.Object);
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
            using (var asiceBuilder = AsiceBuilder.Create(
                zipStream,
                MessageDigestAlgorithm.SHA256,
                ManifestSpec.Cades,
                signingCertificates))
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
            using (var asiceBuilder = AsiceBuilder.Create(
                zipStream,
                MessageDigestAlgorithm.SHA256,
                ManifestSpec.Cades,
                signingCertificates))
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

    [Theory(DisplayName = "AddFileWithPath rejects invalid paths")]
    [InlineData("../small.pdf")]
    [InlineData("root/../../small.pdf")]
    [InlineData("/small.pdf")]
    [InlineData("C:/small.pdf")]
    [InlineData("root\\small.pdf")]
    [InlineData("./small.pdf")]
    [InlineData("root//small.pdf")]
    [InlineData("root/")]
    [InlineData("mimetype")]
    [InlineData("a:b")]
    [InlineData(null)]
    public void TestAddFileWithPathRejectsInvalidPath(string path)
    {
        using var zipStream = new MemoryStream();
        using var asiceBuilder = AsiceBuilder.Create(
            zipStream,
            MessageDigestAlgorithm.SHA256,
            ManifestSpec.Cades,
            null);

        Action addFile = () => asiceBuilder.AddFileWithPath(
            new MemoryStream(new byte[] { 1 }),
            path,
            MimeType.ForString("application/pdf"));

        addFile.ShouldThrow<ArgumentException>().Message.ShouldContain("is not allowed");
    }

    [Theory(DisplayName = "AddFileWithPath rejects the names the package itself uses, whatever their case")]
    [InlineData("META-INF")]
    [InlineData("meta-inf")]
    [InlineData("MIMETYPE")]
    public void TestAddFileWithPathRejectsMetaInf(string path)
    {
        using var zipStream = new MemoryStream();
        using var asiceBuilder = AsiceBuilder.Create(
            zipStream,
            MessageDigestAlgorithm.SHA256,
            ManifestSpec.Cades,
            null);

        Action addFile = () => asiceBuilder.AddFileWithPath(
            new MemoryStream(new byte[] { 1 }),
            path,
            MimeType.ForString("application/pdf"));

        addFile.ShouldThrow<ArgumentException>().Message.ShouldContain("not allowed");
    }

    [Theory(DisplayName = "AddFileWithPath rejects a file that cannot be told apart from another by its file name")]
    [InlineData("a/readme.txt", "b/readme.txt")]
    [InlineData("readme.txt", "a/readme.txt")]
    [InlineData("a/Readme.txt", "b/readme.txt")]
    [InlineData("readme.txt", "readme.txt")]
    public void TestAddFileWithPathRejectsSameFileName(string path, string otherPath)
    {
        using var zipStream = new MemoryStream();
        using var asiceBuilder = AsiceBuilder.Create(
            zipStream,
            MessageDigestAlgorithm.SHA256,
            ManifestSpec.Cades,
            null);
        asiceBuilder.AddFileWithPath(new MemoryStream(new byte[] { 1 }), path, MimeType.ForString("text/plain"));

        Action addFile = () => asiceBuilder.AddFileWithPath(
            new MemoryStream(new byte[] { 2 }),
            otherPath,
            MimeType.ForString("text/plain"));

        addFile.ShouldThrow<ArgumentException>().Message.ShouldContain("the package already has a file named 'readme.txt'");
    }

    [Fact(DisplayName = "AddFile accepts file names that differ only in case in the root folder, as before")]
    public void TestAddFileAcceptsSameFileNameWithDifferentCaseInRoot()
    {
        using var zipStream = new MemoryStream();
        using (var asiceBuilder = AsiceBuilder.Create(
                   zipStream,
                   MessageDigestAlgorithm.SHA256,
                   ManifestSpec.Cades,
                   null))
        {
            asiceBuilder.AddFile(new MemoryStream(new byte[] { 1 }), "Report.txt");
            asiceBuilder.AddFile(new MemoryStream(new byte[] { 2 }), "report.txt");
        }

        using var zipArchive = new ZipArchive(new MemoryStream(zipStream.ToArray()), ZipArchiveMode.Read);
        zipArchive.Entries.Select(e => e.FullName).ShouldContain("Report.txt");
        zipArchive.Entries.Select(e => e.FullName).ShouldContain("report.txt");
    }

    [Fact(DisplayName = "Existing Create overloads still accept null arguments")]
    public void TestExistingCreateOverloadsAcceptNull()
    {
        // Compile-time check: new overloads must not make these calls ambiguous
        using (var zipStream = new MemoryStream())
        using (var asiceBuilder = AsiceBuilder.Create(zipStream, MessageDigestAlgorithm.SHA256, ManifestSpec.Cades, null))
        {
            asiceBuilder.ShouldNotBeNull();
        }

        using (var zipStream = new MemoryStream())
        using (var asiceBuilder = AsiceBuilder.Create(zipStream, MessageDigestAlgorithm.SHA256, ManifestSpec.Cades, null, null))
        {
            asiceBuilder.ShouldNotBeNull();
        }

        using (var zipStream = new MemoryStream())
#pragma warning disable CS0618
        using (var asiceBuilder = AsiceBuilder.Create(zipStream, MessageDigestAlgorithm.SHA256, null))
#pragma warning restore CS0618
        {
            asiceBuilder.ShouldNotBeNull();
        }
    }
}