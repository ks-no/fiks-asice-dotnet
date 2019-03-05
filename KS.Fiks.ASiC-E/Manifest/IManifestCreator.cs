using System.Collections.Generic;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Manifest
{
    public interface IManifestCreator
    {
        ManifestContainer CreateManifest(IEnumerable<AsicePackageEntry> entries);
    }
}