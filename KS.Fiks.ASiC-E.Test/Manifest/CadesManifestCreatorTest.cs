using FluentAssertions;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Manifest
{

    public class CadesManifestCreatorTest
    {
        [Fact]
        public void CreateCadesManifest()
        {
            var cadesManifestCreator = new CadesManifestCreator();
            var digest = MessageDigestAlgorithm.SHA256;
            var entries = new[] { new AsicPackageEntry("my.pdf", MimeType.ForString("application/pdf"), digest)};
            var manifest = cadesManifestCreator.CreateManifest(entries);
            manifest.Should().NotBeNull()
                .And
                .BeOfType<ManifestContainer>();
            manifest.Data.Should().NotBeNull();
            manifest.FileName.Should().Be(CadesManifestCreator.FILENAME);

        }
    }
}