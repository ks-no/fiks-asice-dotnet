using System;
using System.Runtime.ConstrainedExecution;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Security;

namespace KS.Fiks.ASiC_E.Model
{
    public class MessageDigestAlgorithm
    {
        public string Name { get; }

        public Uri Uri { get; }

        public IDigest Digest
        {
            get
            {
                switch (Name)
                {
                    case NameSHA512:
                        return DigestUtilities.GetDigest(NameSHA512);
                    case NameSHA384:
                        return DigestUtilities.GetDigest(NameSHA384);
                    default:
                        return DigestUtilities.GetDigest(NameSHA256);
                }
            }
        }

        private MessageDigestAlgorithm(string name, Uri uri)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public static readonly Uri UriSHA256XmlEnc = new Uri(UriStringSha256XmlEnc);
        public static readonly Uri UriSHA256XmlDsig = new Uri(UriStringSha256XmlDsig);
        public static readonly Uri UriSHA384XmlEnc = new Uri(UriStringSha384XmlEnc);
        public static readonly Uri UriSHA384XmlDsig = new Uri(UriStringSha384XmlDsig);
        public static readonly Uri UriSHA512XmlEnc = new Uri(UriStringSHA512XmlEnc);
        public static readonly Uri UriSHA512XmlDsig = new Uri(UriStringSHA512XmlDsig);
        public static readonly MessageDigestAlgorithm SHA256 = new MessageDigestAlgorithm(NameSHA256, UriSHA256XmlDsig);
        public static readonly MessageDigestAlgorithm SHA384 = new MessageDigestAlgorithm(NameSHA384, UriSHA384XmlDsig);
        public static readonly MessageDigestAlgorithm SHA512 = new MessageDigestAlgorithm(NameSHA512, UriSHA512XmlDsig);

        public static MessageDigestAlgorithm FromUri(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            switch (uri.ToString())
            {
                case UriStringSha256XmlEnc:
                case UriStringSha256XmlDsig:
                    return SHA256;
                case UriStringSha384XmlEnc:
                case UriStringSha384XmlDsig:
                    return SHA384;
                case UriStringSHA512XmlEnc:
                case UriStringSHA512XmlDsig:
                    return SHA512;
                default:
                    return null;
            }
        }

        private const string NameSHA256 = "SHA-256";
        private const string NameSHA384 = "SHA-384";
        private const string NameSHA512 = "SHA-512";
        private const string UriStringSha256XmlDsig = "http://www.w3.org/2000/09/xmldsig#sha256";
        private const string UriStringSha256XmlEnc = "http://www.w3.org/2001/04/xmlenc#sha256";
        private const string UriStringSha384XmlDsig = "http://www.w3.org/2000/09/xmldsig#sha384";
        private const string UriStringSha384XmlEnc = "http://www.w3.org/2001/04/xmlenc#sha384";
        private const string UriStringSHA512XmlDsig = "http://www.w3.org/2000/09/xmldsig#sha512";
        private const string UriStringSHA512XmlEnc = "http://www.w3.org/2001/04/xmlenc#sha512";
    }
}