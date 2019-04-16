using System;

namespace KS.Fiks.ASiC_E.Manifest
{
    public class ManifestReadException : Exception
    {
        public ManifestReadException()
        {
        }

        public ManifestReadException(string message) : base(message)
        {
        }

        public ManifestReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}