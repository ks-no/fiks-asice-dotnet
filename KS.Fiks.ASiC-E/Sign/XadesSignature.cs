using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Sign
{
    public class XadesSignature : ISignatureFileRefCreator
    {
        public SignatureFileRef CreateSignatureRef()
        {
            return new SignatureFileRef(
                AsiceConstants.FileNameSignatureFile,
                AsiceConstants.MimeTypeXadesSignature);
        }
    }
}
