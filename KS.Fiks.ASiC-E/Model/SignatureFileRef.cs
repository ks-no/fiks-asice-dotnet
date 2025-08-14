namespace KS.Fiks.ASiC_E.Model
{
    public class SignatureFileRef : FileRef
    {
        public SignatureFileRef(string fileName)
            : base(fileName, AsiceConstants.MimeTypeCadesSignature)
        {
        }

        public SignatureFileRef(string fileName, MimeType mimeType)
            : base(fileName, mimeType)
        {
        }
    }
}