using System.IO;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Xsd;
using Microsoft.Extensions.Logging;

namespace KS.Fiks.ASiC_E;

public class AsiceVerifier : IAsiceVerifier
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AsiceVerifier> _logger;

    public AsiceVerifier(ILoggerFactory loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<AsiceVerifier>() ??
                  LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AsiceVerifier>();
    }

    public asicManifest Verify(Stream inputData)
    {
        _logger.LogDebug("Verifying ASiC-E archive");

        var asiceReader = new AsiceReader(_loggerFactory);
        using (var ascieReadModel = asiceReader.Read(inputData))
        {
            foreach (var asiceReadEntry in ascieReadModel.Entries)
            {
                using (var entryStream = asiceReadEntry.OpenStream())
                using (var outStream = Stream.Null)
                {
                    entryStream.CopyTo(outStream);
                }
            }

            var verificationResult = ascieReadModel.DigestVerifier.Verification();
            if (!verificationResult.AllValid)
            {
                var invalidFileList =
                    verificationResult.InvalidElements.Aggregate((aggregate, element) => aggregate + "," + element);
                throw new DigestVerificationException(
                    $"Failed to validate digest for the following files {invalidFileList}");
            }

            return ascieReadModel.VerifiedManifest();
        }
    }
}