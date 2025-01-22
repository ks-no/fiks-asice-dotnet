using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class CadesManifestTest
    {
        [Fact(DisplayName = "Instantiate using null value")]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "The whole point is to test that instance creation fails", Scope = "method")]
        public void ProvideNull()
        {
            Action creator = () => new CadesManifest(null);
            creator.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("asiCManifestType");
        }

        [Fact(DisplayName = "Instantiate using a quite empty manifest")]
        public void ProvideWithNoSignatureRef()
        {
            var manifestType = new ASiCManifestType();
            var cadesManifest = new CadesManifest(manifestType);
            cadesManifest.ShouldNotBeNull();
            cadesManifest.Spec.ShouldBe(ManifestSpec.Cades);
            cadesManifest.Digests.ShouldBeNull();
            cadesManifest.SignatureFileName.ShouldBeNull();
            cadesManifest.SignatureFileRef.ShouldBeNull();
        }

        [Fact(DisplayName = "Instantiate with data objects")]
        public void ProvideWithReferences()
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
            cadesManifest.ShouldNotBeNull();
            var digests = cadesManifest.Digests;
            digests.ShouldNotBeNull();
            digests.Count.ShouldBe(1);
            digests.First().Value.MessageDigestAlgorithm.ShouldBeEquivalentTo(digestAlgorithm);
            cadesManifest.SignatureFileRef.ShouldBeNull();
        }

        [Fact(DisplayName = "Instantiate with signature ref")]
        public void ProvideWithSignatureFile()
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
            cadesManifest.ShouldNotBeNull();
            cadesManifest.SignatureFileName.ShouldNotBeNull();
            cadesManifest.SignatureFileName.ShouldBe(SignatureFileName);
            cadesManifest.SignatureFileRef.ShouldNotBeNull();
            cadesManifest.SignatureFileRef.FileName.ShouldBe(SignatureFileName);
            cadesManifest.SignatureFileRef.MimeType.ShouldBe(AsiceConstants.MimeTypeCadesSignature);
        }
    }
}