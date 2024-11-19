using System;
using System.IO;
using System.Linq;
using KS.Fiks.ASiC_E.Xsd;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Cms;

namespace KS.Fiks.ASiC_E.Sign;

public class SignatureVerifier : ISignatureVerifier
{
    private readonly ILogger<SignatureVerifier> _logger;

    public SignatureVerifier(ILoggerFactory loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<SignatureVerifier>() ??
                  LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SignatureVerifier>();
    }

    public certificate Validate(byte[] data, byte[] signature)
    {
        using var dataStream = new MemoryStream(data);
        using var signatureStream = new MemoryStream(signature);
        var cmsTypedStream = new CmsTypedStream(dataStream);
        var cmsSignedDataParser = new CmsSignedDataParser(cmsTypedStream, signatureStream);

        var store = cmsSignedDataParser.GetCertificates();
        var signerInfos = cmsSignedDataParser.GetSignerInfos();
        var signerInfo = signerInfos.GetSigners().First();
        var certificate = store?.EnumerateMatches(signerInfo?.SignerID)
            .FirstOrDefault();

        if (certificate == null)
        {
            return null;
        }

        _logger.LogDebug("Certificate found in signature {dn}", certificate.SubjectDN.ToString());
        var now = DateTime.Now;
        if (now < certificate.NotBefore || now > certificate.NotAfter)
        {
            _logger.LogWarning(
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