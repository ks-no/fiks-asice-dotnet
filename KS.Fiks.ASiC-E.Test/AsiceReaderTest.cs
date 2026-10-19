using System;
using System.IO;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class AsiceReaderTest
    {
        [Fact(DisplayName = "Read from file")]
        public void ReadAndValidateAsice()
        {
            IAsicReader reader = new AsiceReader();
            using (var inputStream = TestDataUtil.ReadValidAsiceCadesFromResource())
            using (var asice = reader.Read(inputStream))
            {
                asice.ShouldNotBeNull();
                foreach (var asiceReadEntry in asice.Entries)
                {
                    using (var entryStream = asiceReadEntry.OpenStream())
                    using (var bufferStream = new MemoryStream())
                    {
                        entryStream.CopyTo(bufferStream);
                        bufferStream.ToArray().Count().ShouldBeGreaterThan(0);
                    }
                }

                asice.DigestVerifier.Verification().AllValid.ShouldBeTrue();
            }
        }

        [Fact(DisplayName = "Entry in a folder has FullName with the path and FileName without")]
        public void ReadFileInFolder()
        {
            var package = BuildPackage("root/someFolder/small.pdf");

            using var asice = new AsiceReader().Read(new MemoryStream(package));
            var entry = asice.Entries.Single();
            entry.FullName.ShouldBe("root/someFolder/small.pdf");
            entry.FileName.ShouldBe("small.pdf");

            using (var entryStream = entry.OpenStream())
            {
                entryStream.CopyTo(Stream.Null);
            }

            asice.DigestVerifier.Verification().AllValid.ShouldBeTrue();
        }

        [Theory(DisplayName = "Package with an entry that has an invalid path is rejected")]
        [InlineData("../file.pdf")]
        [InlineData("root/../../file.pdf")]
        [InlineData("/file.pdf")]
        [InlineData("C:/file.pdf")]
        [InlineData("a:b")]
        [InlineData("root\\file.pdf")]
        [InlineData("./file.pdf")]
        [InlineData("root//file.pdf")]
        [InlineData("META-INF/../file.pdf")]
        [InlineData("../")]
        public void InvalidPathIsRejected(string path)
        {
            var package = AddEntries(BuildPackage("file.txt"), path);

            Action read = () => new AsiceReader().Read(new MemoryStream(package));

            read.ShouldThrow<ArgumentException>().Message.ShouldContain("has an invalid path");
        }

        [Fact(DisplayName = "The error message writes control characters as U+XXXX")]
        public void InvalidPathIsEscapedInTheErrorMessage()
        {
            var package = AddEntries(BuildPackage("file.txt"), "../file\n.pdf");

            Action read = () => new AsiceReader().Read(new MemoryStream(package));

            var message = read.ShouldThrow<ArgumentException>().Message;
            message.ShouldContain("U+000A");
            message.ShouldNotContain("\n");
        }

        [Theory(DisplayName = "Package with a valid path is read and verified, also when the name is not legal on every system")]
        [InlineData("~/file.pdf")]
        [InlineData("$HOME/file.pdf")]
        [InlineData("Søknad om støtte.pdf")]
        [InlineData("a;b`c.pdf")]
        [InlineData("con.pdf")]
        [InlineData("what?.pdf")]
        public void ValidPathIsAccepted(string path)
        {
            var package = BuildPackage(path);

            using var asice = new AsiceReader().Read(new MemoryStream(package));
            asice.Entries.Single().FullName.ShouldBe(path);

            using var inputStream = new MemoryStream(package);
            new AsiceVerifier().Verify(inputStream).ShouldNotBeNull();
        }

        [Fact(DisplayName = "Package with folder entries can be read and verified")]
        public void ReadPackageWithFolderEntries()
        {
            // Zip tools often write an entry for each folder
            var entries = TestDataUtil.ReadEntries(BuildPackage("root/someFolder/file.txt", "docs/mimetype"));
            entries.InsertRange(1, new[] { new ZipEntry("root/", Array.Empty<byte>()), new ZipEntry("root/someFolder/", Array.Empty<byte>()) });
            var package = TestDataUtil.WriteEntries(entries);

            using var asice = new AsiceReader().Read(new MemoryStream(package));
            asice.Entries.Select(e => e.FullName).ShouldBe(new[] { "root/someFolder/file.txt", "docs/mimetype" });

            using var inputStream = new MemoryStream(package);
            new AsiceVerifier().Verify(inputStream).ShouldNotBeNull();
        }

        [Fact(DisplayName = "Reading one file twice and another not at all fails the digest verification")]
        public void ReadingOneFileTwiceIsNotEnough()
        {
            var package = BuildPackage("a.txt", "b.txt");

            using var asice = new AsiceReader().Read(new MemoryStream(package));
            var a = asice.Entries.Single(e => e.FileName == "a.txt");
            for (var i = 0; i < 2; i++)
            {
                using var entryStream = a.OpenStream();
                entryStream.CopyTo(Stream.Null);
            }

            Action verification = () => asice.DigestVerifier.Verification();
            verification.ShouldThrow<DigestVerificationException>().Message.ShouldContain("b.txt");
        }

        [Theory(DisplayName = "Package with two files with the same name, where one is in a folder, is rejected")]
        [InlineData("a/readme.txt", "b/readme.txt")]
        [InlineData("readme.txt", "a/readme.txt")]
        [InlineData("a/Readme.txt", "b/readme.txt")]
        public void SameFileNameInDifferentFoldersIsRejected(string path, string otherPath)
        {
            // The builder does not write such packages, so the entries are added to the zip directly
            var package = AddEntries(BuildPackage("file.txt"), path, otherPath);

            Action read = () => new AsiceReader().Read(new MemoryStream(package));

            read.ShouldThrow<ArgumentException>().Message.ShouldContain("more than one file is named");
        }

        [Fact(DisplayName = "Package with file names that differ only in case in the root folder is read as before")]
        public void SameFileNameWithDifferentCaseInRootIsAccepted()
        {
            var package = BuildPackage("Report.txt", "report.txt");

            using var inputStream = new MemoryStream(package);
            new AsiceVerifier().Verify(inputStream).file.Length.ShouldBe(2);
        }

        [Fact(DisplayName = "File that is not declared in the manifest gives DigestVerificationException")]
        public void UndeclaredFileIsRejected()
        {
            var package = AddEntries(BuildPackage("file.txt"), "extra.txt");

            using var asice = new AsiceReader().Read(new MemoryStream(package));
            Action readEntries = () => asice.Entries.ToList();

            readEntries.ShouldThrow<DigestVerificationException>().Message.ShouldContain("extra.txt");
        }

        private static byte[] AddEntries(byte[] package, params string[] paths)
        {
            var entries = TestDataUtil.ReadEntries(package);
            entries.AddRange(paths.Select(path => new ZipEntry(path, Encoding.UTF8.GetBytes("content"))));
            return TestDataUtil.WriteEntries(entries);
        }

        private static byte[] BuildPackage(params string[] paths)
        {
            using var zipStream = new MemoryStream();
            using (var builder = AsiceBuilder.Create(
                       zipStream,
                       MessageDigestAlgorithm.SHA256,
                       ManifestSpec.Cades,
                       TestdataLoader.ReadCertificatesForTest()))
            {
                foreach (var path in paths)
                {
                    builder.AddFileWithPath(
                        new MemoryStream(Encoding.UTF8.GetBytes(path)),
                        path,
                        MimeType.ForString("text/plain"));
                }
            }

            return zipStream.ToArray();
        }
    }
}
