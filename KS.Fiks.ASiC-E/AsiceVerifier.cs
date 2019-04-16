using System.IO;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E
{
    public class AsiceVerifier : IAsiceVerifier
    {
        public asicManifest Verify(Stream inputData)
        {
            var asiceReader = new AsiceReader();
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
}