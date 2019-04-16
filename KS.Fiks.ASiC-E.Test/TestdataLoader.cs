using System;
using System.IO;
using System.Linq;
using System.Reflection;
using KS.Fiks.ASiC_E.Crypto;

namespace KS.Fiks.ASiC_E.Test
{
    public static class TestdataLoader
    {

        public static ICertificateHolder ReadCertificatesForTest()
        {
            return PreloadedCertificateHolder.Create(ReadPublicKey(), ReadPrivateKey());
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

        private static byte[] ReadPrivateKey()
        {
            return ReadFromResource("fiks_demo_private.pem");
        }

        private static byte[] ReadPublicKey()
        {
            return ReadFromResource("fiks_demo_public.pem");
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