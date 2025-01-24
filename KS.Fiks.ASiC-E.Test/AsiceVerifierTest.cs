using System.IO;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class AsiceVerifierTest
    {
        [Fact(DisplayName = "Verify valid ASiC-E with CADES manifest and signature")]
        public void VerifyValidCades()
        {
            var asiceArchive = TestdataLoader.ReadFromResource("cades-valid.asice");
            IAsiceVerifier asiceVerifier = new AsiceVerifier();
            using (var inputStream = new MemoryStream(asiceArchive))
            {
                var asicManifest = asiceVerifier.Verify(inputStream);
                asicManifest.ShouldNotBeNull();
                asicManifest.certificate.ShouldNotBeNull();
                asicManifest.certificate.Length.ShouldBe(1);
                asicManifest.file.ShouldNotBeNull();
                asicManifest.file.Length.ShouldBe(2);
                asicManifest.rootfile.ShouldBeNull();
            }
        }
    }
}