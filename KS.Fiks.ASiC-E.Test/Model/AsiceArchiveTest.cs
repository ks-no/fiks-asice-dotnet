using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using FluentAssertions;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class AsiceArchiveTest
    {
        private const string FileNameTestPdf = "small.pdf";

        [Fact(DisplayName = "Create ASiC-E package")]
        public void CreateArchive()
        {
            byte[] zippedData;
            using (var zippedOutStream = new MemoryStream())
            {
                using (var archive = AsiceArchive.Create(
                    zippedOutStream,
                    new CadesManifestCreator(),
                    TestdataLoader.ReadCertificatesForTest()))
                using (var fileStream = File.OpenRead(FileNameTestPdf))
                {
                    archive.AddEntry(
                        fileStream,
                        new FileRef(FileNameTestPdf, MimeType.ForString(MimeTypes.GetMimeType(FileNameTestPdf))));
                }

                zippedData = zippedOutStream.ToArray();
                zippedData.Should().NotBeNull();
                zippedData.Should().HaveCountGreaterThan(0);
            }

            using (var zipInput = new MemoryStream(zippedData))
            using (var zippedArchive = new ZipArchive(zipInput, ZipArchiveMode.Read))
            {
                zippedArchive.Entries.Should().HaveCount(4);
                zippedArchive.Entries.First(e => e.FullName.Equals(FileNameTestPdf, StringComparison.CurrentCulture)).Should().NotBeNull();
                zippedArchive.Entries.First(e => e.FullName.Equals(CadesManifestCreator.FILENAME, StringComparison.CurrentCulture)).Should().NotBeNull();
                zippedArchive.Entries.First(e => e.FullName.Equals(AsiceConstants.FileNameMimeType, StringComparison.CurrentCulture)).Should()
                    .NotBeNull();

                var mimeTypeEntry = zippedArchive.GetEntry(AsiceConstants.FileNameMimeType);
                using (var entryStream = mimeTypeEntry.Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);
                    Encoding.UTF8.GetString(copyStream.ToArray()).Should().Be(AsiceConstants.ContentTypeASiCe);
                }

                // Verifies that a CADES manifest has been generated
                using (var entryStream = zippedArchive.GetEntry(CadesManifestCreator.FILENAME).Open())
                using (var copyStream = new MemoryStream())
                {
                    entryStream.CopyTo(copyStream);

                    var manifestXml = Encoding.UTF8.GetString(copyStream.ToArray());
                    manifestXml.Should().NotBeNull();
                    Console.Out.WriteLine("Manifest: {0}", manifestXml);

                }

                var signatureFile = zippedArchive.Entries
                    .First(e => e.FullName.StartsWith("META-INF") && e.FullName.EndsWith(".p7s"));
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
        }
    }
}