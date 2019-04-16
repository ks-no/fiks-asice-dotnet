using System.IO;
using FluentAssertions;
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
                asicManifest.Should().NotBeNull();
                asicManifest.certificate.Should().NotBeNull();
                asicManifest.certificate.Length.Should().Be(1);
                asicManifest.file.Should().NotBeNull();
                asicManifest.file.Length.Should().Be(2);
                asicManifest.rootfile.Should().BeNull();
            }
        }
    }
}