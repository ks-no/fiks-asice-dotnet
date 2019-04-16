using System.Collections.Generic;
using System.Linq;

namespace KS.Fiks.ASiC_E.Model
{
    public class ManifestContainer
    {
        public string FileName { get; }

        public SignatureFileRef SignatureFileRef { get; set; }

        public IEnumerable<byte> Data { get; }

        public ManifestSpec ManifestSpec { get; }

        public ManifestContainer(string fileName, IEnumerable<byte> data, SignatureFileRef signatureFileRef, ManifestSpec manifestSpec)
        {
            FileName = fileName;
            Data = data ?? Enumerable.Empty<byte>();
            SignatureFileRef = signatureFileRef;
            ManifestSpec = manifestSpec;
        }
    }
}