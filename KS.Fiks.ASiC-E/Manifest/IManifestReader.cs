using System.IO;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Manifest
{
    public interface IManifestReader<M> where M : AbstractManifest
    {
        M FromStream(Stream stream);
    }
}