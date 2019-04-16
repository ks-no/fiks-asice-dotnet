using System;

namespace KS.Fiks.ASiC_E.Sign
{
    public class SignatureVerificationException : Exception
    {
        public SignatureVerificationException()
        {
        }

        public SignatureVerificationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SignatureVerificationException(string message)
            : base(message)
        {
        }
    }
}