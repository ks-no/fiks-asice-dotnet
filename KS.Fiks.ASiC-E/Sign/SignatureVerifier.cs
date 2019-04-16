using System;
using System.Globalization;
using System.IO;
using System.Linq;
using KS.Fiks.ASiC_E.Xsd;
using NLog;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;

namespace KS.Fiks.ASiC_E.Sign
{
    public class SignatureVerifier : ISignatureVerifier
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public certificate Validate(byte[] data, byte[] signature)
        {
            using (var dataStream = new MemoryStream(data))
            using (var signatureStream = new MemoryStream(signature))
            {
                var cmsTypedStream = new CmsTypedStream(dataStream);
                var cmsSignedDataParser = new CmsSignedDataParser(cmsTypedStream, signatureStream);

                var store = cmsSignedDataParser.GetCertificates("Collection");
                var signerInfos = cmsSignedDataParser.GetSignerInfos();
                var signerInfo = signerInfos.GetSigners().OfType<SignerInformation>().First();
                var certificate = store?.GetMatches(signerInfo?.SignerID)
                    .OfType<X509Certificate>().FirstOrDefault();

                if (certificate == null)
                {
                    return null;
                }

                Logger.Debug(
                    CultureInfo.CurrentCulture,
                    "Certificate found in signature {dn}",
                    certificate.SubjectDN.ToString());
                var now = DateTime.Now;
                if (now < certificate.NotBefore || now > certificate.NotAfter)
                {
                    Logger.Info(
                        CultureInfo.CurrentCulture,
                        "The certificate is not valid right now as it is only valid between {startTime}-{endTime}",
                        certificate.NotBefore,
                        certificate.NotAfter);
                }

                return new certificate
                {
                    subject = certificate.SubjectDN.ToString(),
                    certificate1 = certificate.GetEncoded()
                };
            }
        }
    }
}