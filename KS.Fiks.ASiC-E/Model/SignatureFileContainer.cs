using System;
using System.Collections.Generic;

namespace KS.Fiks.ASiC_E.Model
{
    public class SignatureFileContainer
    {
        public SignatureFileRef SignatureFileRef { get; }

        public IEnumerable<byte> Data { get; }

        public SignatureFileContainer(SignatureFileRef signatureFileRef, IEnumerable<byte> data)
        {
            SignatureFileRef = signatureFileRef ?? throw new ArgumentNullException(nameof(signatureFileRef));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }
}