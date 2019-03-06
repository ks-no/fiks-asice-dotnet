using System;
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

        public static readonly MessageDigestAlgorithm SHA256 = new MessageDigestAlgorithm(NameSHA256, new Uri(UriStringSHA256));
        public static readonly MessageDigestAlgorithm SHA384 = new MessageDigestAlgorithm(NameSHA384, new Uri(UriStringSHA384));
        public static readonly MessageDigestAlgorithm SHA512 = new MessageDigestAlgorithm(NameSHA512, new Uri(UriStringSHA512));

        private const string NameSHA256 = "SHA-256";
        private const string NameSHA384 = "SHA-384";
        private const string NameSHA512 = "SHA-512";
        private const string UriStringSHA256 = "http://www.w3.org/2001/04/xmlenc#sha256";
        private const string UriStringSHA384 = "http://www.w3.org/2001/04/xmlenc#sha384";
        private const string UriStringSHA512 = "http://www.w3.org/2001/04/xmlenc#sha512";
    }
}