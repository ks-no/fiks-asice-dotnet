namespace KS.Fiks.ASiC_E.Model
{
    public class ManifestContainer
    {
        public string FileName { get; }

        public SignatureFileRef SignatureFileRef { get; }

        public byte[] Data { get; }

        public ManifestSpec ManifestSpec { get; }

        public ManifestContainer(string fileName, byte[] data, SignatureFileRef signatureFileRef, ManifestSpec manifestSpec)
        {
            FileName = fileName;
            Data = data;
            SignatureFileRef = signatureFileRef;
            ManifestSpec = manifestSpec;
        }
    }
}