namespace KS.Fiks.ASiC_E.Model
{
    public class ManifestContainer
    {
        public string FileName { get; }

        public SignatureFileRef SignatureFileRef { get; }

        public byte[] Data { get; }

        public ManifestContainer(string fileName, byte[] data, SignatureFileRef signatureFileRef)
        {
            FileName = fileName;
            Data = data;
            SignatureFileRef = signatureFileRef;
        }
    }
}