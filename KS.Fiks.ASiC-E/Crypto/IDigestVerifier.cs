using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Crypto
{
    /// <summary>
    /// Common contract for digest value verifiers
    /// </summary>
    public interface IDigestVerifier
    {
        /// <summary>
        /// Used to check the status of the verifier after all the processing has been completed
        /// </summary>
        /// <exception cref="DigestVerificationException">Thrown if the verification is inconsistent, eg if the number of declared digest is not equal to the number of files processed</exception>
        /// <returns>an DigestVerificationResult that contains a summary of all the verifications that has been performed</returns>
        DigestVerificationResult Verification();
    }
}