using KS.Fiks.ASiC_E.Sign;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Sign
{
    public class SignatureVerifierTest
    {
        [Fact(DisplayName = "Verify signature")]
        public void VerifySignature()
        {
            var signature = TestdataLoader.ReadFromResource("signature.p7s");
            var signedData = TestdataLoader.ReadFromResource("signedData.xml");
            var signatureVerifier = new SignatureVerifier();
            var certificate = signatureVerifier.Validate(signedData, signature);
            certificate.ShouldNotBeNull();
        }
    }
}