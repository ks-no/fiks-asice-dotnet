using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class DeclaredDigestFileTest
    {
        private const string FileName = "filename.data";
        private static readonly MimeType MimeType = MimeType.ForString("application/octet-stream");

        [Fact(DisplayName = "Verify equal digest and algorithm")]
        public void VerifyEquals()
        {
            var calculatedDigest = new byte[] { 0, 1, 0, 1 };
            var digest = new DeclaredDigestFile(calculatedDigest, MessageDigestAlgorithm.SHA256, FileName, MimeType);
            digest.Verify(new DeclaredDigestFile(calculatedDigest, digest.MessageDigestAlgorithm, FileName, MimeType)).ShouldBeTrue();
        }

        [Fact(DisplayName = "Verify equal digest but not algorithm")]
        public void VerifyEqualsDifferentAlgorithm()
        {
            var calculatedDigest = new byte[] { 0, 1, 0, 1 };
            var digest = new DeclaredDigestFile(calculatedDigest, MessageDigestAlgorithm.SHA256, FileName, MimeType);
            digest.Verify(new DeclaredDigestFile(calculatedDigest, MessageDigestAlgorithm.SHA384, FileName, MimeType)).ShouldBeFalse();
        }

        [Fact(DisplayName = "Verify different digest and equal algorithm")]
        public void VerifyEqualAlgorithmButDifferentDigest()
        {
            new DeclaredDigestFile(new byte[] { 0, 0, 0, 0 }, MessageDigestAlgorithm.SHA512, FileName, MimeType)
                .Verify(new DeclaredDigestFile(new byte[] { 1, 1, 1, 1 }, MessageDigestAlgorithm.SHA512, FileName, MimeType)).ShouldBeFalse();
        }
    }
}