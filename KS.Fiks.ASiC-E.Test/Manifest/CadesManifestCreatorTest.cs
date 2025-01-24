using System.IO;
using System.Linq;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Manifest
{
    public class CadesManifestCreatorTest
    {
        [Fact(DisplayName = "Create CAdES manifest without signature")]
        public void CreateCadesManifest()
        {
            var cadesManifestCreator = CadesManifestCreator.CreateWithoutSignatureFile();
            var digestAlgorithm = MessageDigestAlgorithm.SHA256;
            var fileEntry = new AsicePackageEntry("my.pdf", MimeType.ForString("application/pdf"), digestAlgorithm);
            fileEntry.Digest = new DigestContainer(new byte[] { 0, 0, 1 }, digestAlgorithm);
            var entries = new[] { fileEntry };
            var manifest = cadesManifestCreator.CreateManifest(entries);
            manifest.ShouldNotBeNull()
                .ShouldBeOfType<ManifestContainer>();
            manifest.Data.ShouldNotBeNull();
            manifest.FileName.ShouldBe(AsiceConstants.CadesManifestFilename);

            var xmlManifest = DeserializeManifest(manifest.Data.ToArray());
            xmlManifest.ShouldNotBeNull();
            xmlManifest.SigReference.ShouldBeNull();
            xmlManifest.DataObjectReference.ShouldHaveSingleItem();
            var dataObjectRef = xmlManifest.DataObjectReference.Single();
            dataObjectRef.ShouldNotBeNull();
            dataObjectRef.MimeType.ShouldBe(fileEntry.Type.ToString());
            dataObjectRef.DigestValue.ShouldBe(fileEntry.Digest.GetDigest());
            dataObjectRef.URI.ShouldBe(fileEntry.FileName);
        }

        [Fact(DisplayName = "Create CAdES manifest with signature")]
        public void CreateCadesManifestIncludingSignature()
        {
            var cadesManifestCreator = CadesManifestCreator.CreateWithSignatureFile();
            var fileEntry = new AsicePackageEntry("my.pdf", MimeType.ForString("application/pdf"), MessageDigestAlgorithm.SHA256);
            fileEntry.Digest = new DigestContainer(new byte[] { 0, 0, 1 }, MessageDigestAlgorithm.SHA256);
            var manifest = cadesManifestCreator.CreateManifest(new[] { fileEntry });
            manifest.ShouldNotBeNull().ShouldBeOfType<ManifestContainer>();
            manifest.FileName.ShouldBe(AsiceConstants.CadesManifestFilename);
            var xmlManifest = DeserializeManifest(manifest.Data.ToArray());
            xmlManifest.ShouldNotBeNull();
            xmlManifest.SigReference.ShouldNotBeNull();
            xmlManifest.SigReference.MimeType.ShouldBe(AsiceConstants.ContentTypeSignature);
            xmlManifest.DataObjectReference.ShouldHaveSingleItem();
        }

        private static ASiCManifestType DeserializeManifest(byte[] data)
        {
            using (var xmlStream = new MemoryStream(data))
            {
                var xmlSerializer = new XmlSerializer(typeof(ASiCManifestType));
                var xmlObj = xmlSerializer.Deserialize(xmlStream);
                return (ASiCManifestType)xmlObj;
            }
        }
    }
}