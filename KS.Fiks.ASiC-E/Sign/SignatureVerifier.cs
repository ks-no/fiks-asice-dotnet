using System;
using System.Linq;
using KS.Fiks.ASiC_E.Xsd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;

namespace KS.Fiks.ASiC_E.Sign;

public class SignatureVerifier : ISignatureVerifier
{
    private readonly ILogger<SignatureVerifier> _logger;

    public SignatureVerifier(ILoggerFactory loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SignatureVerifier>();
    }

    /// <summary>
    /// Verifies a detached CMS signature over the given data. Every signer in the signature must be valid.
    /// The trust chain of the signing certificate (CA and revocation) is not checked; that is the
    /// responsibility of the caller.
    /// </summary>
    /// <param name="data">The signed data, for example the ASiC-E manifest</param>
    /// <param name="signature">The detached CMS signature (.p7s)</param>
    /// <returns>The certificate of the first signer</returns>
    /// <exception cref="SignatureVerificationException">If the signature can not be read, has no signers,
    /// lacks the certificate of a signer, or does not match the data</exception>
    public certificate Validate(byte[] data, byte[] signature)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (signature == null)
        {
            throw new ArgumentNullException(nameof(signature));
        }

        var certificate = VerifySigners(data, signature);

        _logger.LogDebug("Certificate found in signature {dn}", certificate.SubjectDN.ToString());
        var now = DateTime.UtcNow;
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

    private static X509Certificate VerifySigners(byte[] data, byte[] signature)
    {
        try
        {
            // Always verify against the given data, even if the signature carries content of its own
            var signedData = new CmsSignedData(new CmsProcessableByteArray(data), signature);
            var signers = signedData.GetSignerInfos().GetSigners();
            if (signers.Count == 0)
            {
                throw new SignatureVerificationException("The signature has no signers");
            }

            var certificates = signedData.GetCertificates();
            X509Certificate firstCertificate = null;
            foreach (var signer in signers)
            {
                var certificate = certificates.EnumerateMatches(signer.SignerID).FirstOrDefault()
                                  ?? throw new SignatureVerificationException(
                                      "The signature does not contain the certificate of the signer");

                // Also checks that the certificate was valid at the signing time, if the signature has one
                if (!signer.Verify(certificate))
                {
                    throw new SignatureVerificationException("The signature does not match the signed data");
                }

                firstCertificate ??= certificate;
            }

            return firstCertificate;
        }
        catch (Exception e) when (e is not SignatureVerificationException)
        {
            // The signature comes from the sender, so any failure to read or verify it means it is invalid
            throw new SignatureVerificationException("The signature could not be verified", e);
        }
    }
}
