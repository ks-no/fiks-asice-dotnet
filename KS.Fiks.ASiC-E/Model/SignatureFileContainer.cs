using System;

namespace KS.Fiks.ASiC_E.Model
{
    public class SignatureFileContainer
    {
        public SignatureFileRef SignatureFileRef { get; }

        public byte[] Data { get; }

        public SignatureFileContainer(SignatureFileRef signatureFileRef, byte[] data)
        {
            SignatureFileRef = signatureFileRef ?? throw new ArgumentNullException(nameof(signatureFileRef));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }
}