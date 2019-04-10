using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class DeclaredDigestTest
    {
        [Fact(DisplayName = "Verify equal digest and algorithm")]
        public void VerifyEquals()
        {
            var calculatedDigest = new byte[] { 0, 1, 0, 1 };
            var digest = new DeclaredDigest(calculatedDigest, MessageDigestAlgorithm.SHA256);
            digest.Verify(new DeclaredDigest(calculatedDigest, digest.MessageDigestAlgorithm)).Should().BeTrue();
        }

        [Fact(DisplayName = "Verify equal digest but not algorithm")]
        public void VerifyEqualsDifferentAlgorithm()
        {
            var calculatedDigest = new byte[] { 0, 1, 0, 1 };
            var digest = new DeclaredDigest(calculatedDigest, MessageDigestAlgorithm.SHA256);
            digest.Verify(new DeclaredDigest(calculatedDigest, MessageDigestAlgorithm.SHA384)).Should().BeFalse();
        }

        [Fact(DisplayName = "Verify different digest and equal algorithm")]
        public void VerifyEqualAlgorithmButDifferentDigest()
        {
            new DeclaredDigest(new byte[] { 0, 0, 0, 0 }, MessageDigestAlgorithm.SHA512)
                .Verify(new DeclaredDigest(new byte[] { 1, 1, 1, 1 }, MessageDigestAlgorithm.SHA512)).Should()
                .BeFalse();
        }
    }
}