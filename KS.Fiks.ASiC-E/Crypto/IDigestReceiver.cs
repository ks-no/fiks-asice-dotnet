namespace KS.Fiks.ASiC_E.Crypto
{
    public interface IDigestReceiver
    {
        void ReceiveDigest(string fileName, byte[] digest);
    }
}