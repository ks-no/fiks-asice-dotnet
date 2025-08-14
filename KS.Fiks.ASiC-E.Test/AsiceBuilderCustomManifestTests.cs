using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test;

public class AsiceBuilderCustomManifestTests
{
    private static readonly XNamespace _ns = XNamespace.Get(
        "http://example.invalid/dummy-manifest-namespace");

    [Fact]
    public void CreateAsicWithCustomManifestWithXades()
    {
        CreateAsicWithCustomManifest(ManifestSpec.Xades);
    }

    [Fact]
    public void CreateAsicWithCustomManifestWithCades()
    {
        CreateAsicWithCustomManifest(ManifestSpec.Cades);
    }

    private void CreateAsicWithCustomManifest(ManifestSpec spec)
    {
        var certHolder = TestdataLoader.ReadCertificatesForTest();

        Func<IManifestCreator> makeManifestCreator =
            () => new CustomManifestFormat(spec);

        using var zipStream = new MemoryStream();

        using (var asiceBuilder = AsiceBuilder.Create(
            zipStream,
            MessageDigestAlgorithm.SHA256,
            spec,
            certHolder,
            makeManifestCreator))
        {
            asiceBuilder.ShouldNotBeNull();

            // We add three files: One introduction file, and two attachments:
            asiceBuilder.AddFile(
                new MemoryStream(
                    Encoding.UTF8.GetBytes(
                        "Please find the requested documents attached.")),
                "intro.txt");

            using (var attachment1= File.OpenRead("small.pdf"))
            {
                asiceBuilder.AddFile(attachment1, "attachment1.pdf");
            }

            using (var attachment2 = File.OpenRead("small.pdf"))
            {
                asiceBuilder.AddFile(attachment2, "attachment2.odt");
            }

            var asiceArchive = asiceBuilder.Build();
            asiceArchive.ShouldNotBeNull();
        }

        byte[] zippedBytes = zipStream.ToArray();
        zippedBytes.Length.ShouldBeGreaterThan(0);

        bool foundUnknownEntry = false;
        bool foundManifest = false;
        using (var zipStream_ = new MemoryStream(zippedBytes))
        using (var zipArchive = new ZipArchive(zipStream_, ZipArchiveMode.Read))
        {
            zipArchive.Entries.Count.ShouldBe(6);

            var invCult = StringComparison.InvariantCulture;

            Func<string, bool> findCadesSignature = (filepath) =>
                filepath.StartsWith("META-INF/signature-", invCult) &&
                filepath.EndsWith(".p7s", invCult);

            Func<string, bool> findXadesSignature = (filepath) =>
                filepath == "META-INF/signatures.xml";

            string expectedManifest = """
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns="http://example.invalid/dummy-manifest-namespace">
              <maindocument href="intro.txt" mime="text/plain" />
              <attachment href="attachment1.pdf" mime="application/pdf" />
              <attachment href="attachment2.odt" mime="application/vnd.oasis.opendocument.text" />
            </manifest>
            """;

            foreach (var entry in zipArchive.Entries)
            {
                var filepath = entry.FullName;
                switch (filepath)
                {
                    case "mimetype": break;
                    case "intro.txt": break;
                    case "attachment1.pdf": break;
                    case "attachment2.odt": break;
                    case "META-INF/customManifest.xml":
                        using (var manifestStream = entry.Open())
                        using (var copyStream = new MemoryStream())
                        {
                            foundManifest = true;
                            manifestStream.CopyTo(copyStream);
                            var manifestBytes = copyStream.ToArray();
                            manifestBytes.Length.ShouldBeGreaterThan(0);
                            var manifestContent = Encoding.UTF8.GetString(manifestBytes);
                            manifestContent.ShouldBe(
                                expectedManifest,
                                StringCompareShould.IgnoreLineEndings);
                        }

                        break;
                    default:
                        bool foundSignature = (spec == ManifestSpec.Cades
                            ? findCadesSignature
                            : findXadesSignature)(filepath);

                        if (!foundSignature)
                        {
                            foundUnknownEntry = true;
                        }

                        break;
                }
            }
        }

        foundUnknownEntry.ShouldBeFalse();
        foundManifest.ShouldBeTrue();
    }

    private class CustomManifestFormat : IManifestCreator
    {
        private readonly ManifestSpec _spec;

        public CustomManifestFormat(ManifestSpec spec)
        {
            _spec = spec;
        }

        public ManifestContainer CreateManifest(
            IEnumerable<AsicePackageEntry> entries,
            SignatureFileRef signatureFileRef)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
            };

            AsicePackageEntry[] entriesArray = entries?.ToArray();
            AsicePackageEntry mainEntry = entriesArray[0];
            AsicePackageEntry[] attachments = entriesArray[1..];

            XDocument doc = new XDocument(
                new XElement(
                    _ns + "manifest",
                    new XAttribute("xmlns", _ns),
                    MakePayloadElement(true, mainEntry),
                    attachments.Select(
                        attachment => MakePayloadElement(false, attachment))));

            using var memStream = new MemoryStream();

            using (var writer = XmlWriter.Create(memStream, settings))
            {
                doc.Save(writer);
            }

            byte[] bytes = memStream.ToArray();

            return new ManifestContainer(
                fileName: "META-INF/customManifest.xml",
                data: bytes,
                signatureFileRef: signatureFileRef,
                manifestSpec: _spec);
        }

        private XElement MakePayloadElement(
            bool isMainDocument,
            AsicePackageEntry attachment)
        {
            string elemName = isMainDocument ? "maindocument" : "attachment";

            return new XElement(
                _ns + elemName,
                new XAttribute("href", attachment.FileName),
                new XAttribute("mime", attachment.Type.ToString()));
        }
    }
}
