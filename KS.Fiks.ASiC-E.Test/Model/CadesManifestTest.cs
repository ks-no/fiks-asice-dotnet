using System;
using System.Linq;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class CadesManifestTest
    {
        [Fact(DisplayName = "Instantiate using null value")]
        public void provideNull()
        {
            Action creator = () => new CadesManifest(null);
            creator.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("asiCManifestType");
        }

        [Fact(DisplayName = "Instantiate using a quite empty manifest")]
        public void provideWithNoSignatureRef()
        {
            var manifestType = new ASiCManifestType();
            var cadesManifest = new CadesManifest(manifestType);
            cadesManifest.Should().NotBeNull();
            cadesManifest.Digests.Should().BeNull();
            cadesManifest.SignatureFileName.Should().BeNull();
            cadesManifest.SignatureFileRef.Should().BeNull();
        }

        [Fact(DisplayName = "Instantiate with data objects")]
        public void provideWithReferences()
        {
            const string FileName = "filename.txt";
            var digestAlgorithm = MessageDigestAlgorithm.SHA256;
            var manifestType = new ASiCManifestType
            {
                DataObjectReference = new[]
                {
                    new DataObjectReferenceType
                    {
                        Rootfile = false,
                        MimeType = "text/plain",
                        URI = FileName,
                        DigestMethod = new DigestMethodType
                        {
                            Algorithm = digestAlgorithm.Uri.ToString()
                        },
                        DigestValue = new byte[] { 0, 1, 0, 1 }
                    }
                }
            };
            var cadesManifest = new CadesManifest(manifestType);
            cadesManifest.Should().NotBeNull();
            var digests = cadesManifest.Digests;
            digests.Should().NotBeNull();
            digests.Count().Should().Be(1);
            digests.First().MessageDigestAlgorithm.Should().BeEquivalentTo(digestAlgorithm);
            cadesManifest.SignatureFileRef.Should().BeNull();
        }

        [Fact(DisplayName = "Instantiate with signature ref")]
        public void provideWithSignatureFile()
        {
            const string SignatureFileName = "my.p7";
            var manifestType = new ASiCManifestType
            {
                SigReference = new SigReferenceType
                {
                    MimeType = AsiceConstants.ContentTypeSignature,
                    URI = SignatureFileName
                }
            };
            var cadesManifest = new CadesManifest(manifestType);
            cadesManifest.Should().NotBeNull();
            cadesManifest.SignatureFileName.Should().NotBeNull();
            cadesManifest.SignatureFileName.Should().Be(SignatureFileName);
            cadesManifest.SignatureFileRef.Should().NotBeNull();
            cadesManifest.SignatureFileRef.FileName.Should().Be(SignatureFileName);
            cadesManifest.SignatureFileRef.MimeType.Should().Be(AsiceConstants.MimeTypeCadesSignature);
        }
    }
}