using System;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace KS.Fiks.ASiC_E.Crypto
{
    /*
     * Holds private public key provided from a System X509Certificate2 instance
     */
    public class SystemX509CertificateHolder : ICertificateHolder
    {
        public AsymmetricKeyParameter GetPrivateKey()
        {
            return Key;
        }

        public X509Certificate GetPublicCertificate()
        {
            return Cert;
        }

        private X509Certificate Cert { get; }

        private AsymmetricKeyParameter Key { get; }

        public SystemX509CertificateHolder(X509Certificate2 x509)
        {
            if (x509 == null)
            {
                throw new ArgumentNullException(nameof(x509));
            }

            Cert = DotNetUtilities.FromX509Certificate(x509);
            var pair = DotNetUtilities.GetKeyPair(x509.PrivateKey);
            Key = pair.Private;
        }
    }
}