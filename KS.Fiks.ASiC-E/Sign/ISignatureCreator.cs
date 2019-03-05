using System.Collections.Generic;
using System.Runtime.InteropServices;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Sign
{
    public interface ISignatureCreator
    {
        SignatureFileContainer CreateSignatureFile(IEnumerable<AsicePackageEntry> asicPackageEntries);

        SignatureFileContainer CreateCadesSignatureFile(ManifestContainer manifestContainer);
    }
}