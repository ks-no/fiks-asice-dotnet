using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KS.Fiks.ASiC_E.Crypto;

namespace KS.Fiks.ASiC_E.Test
{
    public static class TestdataLoader
    {
        private static string _fiksDemoPrivatePem = "fiks_demo_private.pem";
        private static string _fiksDemoPublicPem = "fiks_demo_public.pem";

        public static ICertificateHolder ReadCertificatesForTest()
        {
            return PreloadedCertificateHolder.Create(ReadPublicKey(), ReadPrivateKey());
        }

        public static ICertificateHolder ReadX509Certificate2ForTest()
        {
            var certificate = new X509Certificate2(ReadPublicKey());
            var rsa = RSA.Create();
            rsa.ImportFromPem(ReadStringFromResource(_fiksDemoPrivatePem));
            return PreloadedCertificateHolder.Create(certificate.CopyWithPrivateKey(rsa));
        }

        public static byte[] ReadFromResource(string resource)
        {
            using (var inputStream = LoadFromAssembly(resource))
            using (var copyStream = new MemoryStream())
            {
                inputStream.CopyTo(copyStream);
                return copyStream.ToArray();
            }
        }

        public static string ReadStringFromResource(string resource)
        {
            using (var inputStream = LoadFromAssembly(resource))
            {
                using (var reader = new StreamReader(inputStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static byte[] ReadPrivateKey()
        {
            return ReadFromResource(_fiksDemoPrivatePem);
        }

        private static byte[] ReadPublicKey()
        {
            return ReadFromResource(_fiksDemoPublicPem);
        }

        private static Stream LoadFromAssembly(string resource)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(str =>
                str.EndsWith(resource, StringComparison.CurrentCulture));
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}