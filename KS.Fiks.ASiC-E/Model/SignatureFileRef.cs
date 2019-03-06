namespace KS.Fiks.ASiC_E.Model
{
    public class SignatureFileRef : FileRef
    {
        public SignatureFileRef(string fileName)
            : base(fileName, AsiceConstants.MimeTypeCadesSignature)
        {
        }
    }
}