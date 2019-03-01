using System;

namespace KS.Fiks.ASiC_E.Model
{
    public class DigestContainer
    {
        private readonly byte[] _digest;
        private readonly MessageDigestAlgorithm _messageDigestAlgorithm;

        public DigestContainer(byte[] digest, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            this._digest = digest ?? throw new ArgumentNullException(nameof(digest));
            this._messageDigestAlgorithm = messageDigestAlgorithm ??
                                           throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        }

        public byte[] GetDigest()
        {
            return this._digest;
        }

        public MessageDigestAlgorithm GetMessageDigestAlgorithm()
        {
            return this._messageDigestAlgorithm;
        }
    }
}