using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace KS.Fiks.ASiC_E.Crypto
{
    public interface ICertificateHolder
    {
        AsymmetricKeyParameter GetPrivateKey();

        IReadOnlyList<X509Certificate> GetCertificateChain();

        X509Certificate GetPublicCertificate();
    }

    /**
     * Holds certificate and private key provided from PEM files
     */
    public class PreloadedCertificateHolder : ICertificateHolder
    {
        private X509Certificate Certificate { get; }

        private AsymmetricKeyParameter PrivateKey { get; }

        private IReadOnlyList<X509Certificate> CertificateChain { get; }

        private PreloadedCertificateHolder(
            X509Certificate certificate,
            AsymmetricKeyParameter privateKey,
            List<X509Certificate> certificateChain = null)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
            CertificateChain = certificateChain ?? new List<X509Certificate>();
        }

        public static PreloadedCertificateHolder Create(byte[] pemPublicCertificate, byte[] pemPrivateKey)
        {
            return new PreloadedCertificateHolder(ExtractX509Certificate(pemPublicCertificate), ExtractPrivateKey(pemPrivateKey));
        }

        public static PreloadedCertificateHolder Create(
            X509Certificate2 x509Certificate2,
            IEnumerable<X509Certificate2> certificateChain = null)
        {
            // Convert chain's System certificates to BouncyCastle certificates:
            List<X509Certificate> convertedChain =
                (certificateChain ?? Array.Empty<X509Certificate2>())
                .Select(DotNetUtilities.FromX509Certificate)
                .ToList();

            return new PreloadedCertificateHolder(
                DotNetUtilities.FromX509Certificate(x509Certificate2),
                DotNetUtilities.GetKeyPair(x509Certificate2.PrivateKey).Private,
                convertedChain);
        }

        public AsymmetricKeyParameter GetPrivateKey()
        {
            return PrivateKey;
        }

        public X509Certificate GetPublicCertificate()
        {
            return Certificate;
        }

        public IReadOnlyList<X509Certificate> GetCertificateChain()
        {
            return CertificateChain;
        }

        private static X509Certificate ExtractX509Certificate(byte[] pemPublicCertificate)
        {
            var certificatePem = ReadPem(pemPublicCertificate);
            if (!certificatePem.Type.EndsWith("CERTIFICATE", StringComparison.CurrentCulture))
            {
                throw new ArgumentException("The PEM data is not a valid certificate", nameof(pemPublicCertificate));
            }

            return new X509Certificate(X509CertificateStructure.GetInstance(certificatePem.Content));
        }

        private static AsymmetricKeyParameter ExtractPrivateKey(byte[] pemPrivateKey)
        {
            var pemObject = ReadPem(pemPrivateKey);
            if (!pemObject.Type.EndsWith("RSA PRIVATE KEY", StringComparison.CurrentCulture))
            {
                return PrivateKeyFactory.CreateKey(pemObject.Content);
            }

            var rsaPrivateKeyStructure = RsaPrivateKeyStructure.GetInstance(pemObject.Content);
            return new RsaPrivateCrtKeyParameters(
                rsaPrivateKeyStructure.Modulus,
                rsaPrivateKeyStructure.PublicExponent,
                rsaPrivateKeyStructure.PrivateExponent,
                rsaPrivateKeyStructure.Prime1,
                rsaPrivateKeyStructure.Prime2,
                rsaPrivateKeyStructure.Exponent1,
                rsaPrivateKeyStructure.Exponent2,
                rsaPrivateKeyStructure.Coefficient);
        }

        private static PemObject ReadPem(byte[] pemString)
        {
            using (var pemPublicStringReader = new StringReader(Encoding.UTF8.GetString(pemString)))
            {
                return new PemReader(pemPublicStringReader).ReadPemObject();
            }
        }
    }
}