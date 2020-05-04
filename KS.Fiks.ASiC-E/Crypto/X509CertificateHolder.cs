using System;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace KS.Fiks.ASiC_E.Crypto
{
    public class X509CertificateHolder : ICertificateHolder
    {
        public AsymmetricKeyParameter GetPrivateKey()
        {
            return this.Key;
        }

        public X509Certificate GetPublicCertificate()
        {
            return this.Cert;
        }
	
        public X509Certificate Cert { get; set; }
        public AsymmetricKeyParameter Key { get; set; }
	
        public X509CertificateHolder(X509Certificate2 x509)
        {
            if (x509 == null)
            {
                throw new ArgumentNullException(nameof(x509));
            }

            this.Cert = DotNetUtilities.FromX509Certificate(x509);
            var pair = DotNetUtilities.GetKeyPair(x509.PrivateKey);
            this.Key = pair.Private;
        }
    }
}