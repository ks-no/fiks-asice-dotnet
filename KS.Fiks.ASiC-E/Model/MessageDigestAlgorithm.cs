using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;

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
                        return new Sha512Digest();
                    case NameSHA384:
                        return new Sha384Digest();
                    default:
                        return new Sha256Digest();
                }
            }
        }

        private MessageDigestAlgorithm(string name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }

        public static readonly MessageDigestAlgorithm SHA256 = new MessageDigestAlgorithm(NameSHA256, UriSHA256);
        public static readonly MessageDigestAlgorithm SHA384 = new MessageDigestAlgorithm(NameSHA384, UriSHA384);
        public static readonly MessageDigestAlgorithm SHA512 = new MessageDigestAlgorithm(NameSHA512, UriSHA512);

        private const string NameSHA256 = "SHA-256";
        private const string NameSHA384 = "SHA-384";
        private const string NameSHA512 = "SHA-512";
        private static readonly Uri UriSHA256 = new Uri("http://www.w3.org/2001/04/xmlenc#sha256");
        private static readonly Uri UriSHA384 = new Uri("http://www.w3.org/2001/04/xmlenc#sha384");
        private static readonly Uri UriSHA512 = new Uri("http://www.w3.org/2001/04/xmlenc#sha512");
    }
}