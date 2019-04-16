using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E.Sign
{
    public interface ISignatureVerifier
    {
        certificate Validate(byte[] data, byte[] signature);
    }
}