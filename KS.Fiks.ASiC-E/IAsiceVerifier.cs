using System.IO;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E
{
    public interface IAsiceVerifier
    {
        /// <summary>
        /// Verify a given ASiC archive
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        asicManifest Verify(Stream inputData);
    }
}