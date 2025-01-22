using System;
using System.Collections.Immutable;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Crypto
{
    public class DigestVerifierTest
    {
        private static readonly MimeType MimeType = MimeType.ForString("text/plain");

        private static readonly DeclaredDigestFile FileOne = new DeclaredDigestFile(
            new byte[] { 0, 1, 1 },
            MessageDigestAlgorithm.SHA256,
            "fileOne.txt",
            MimeType);

        private static readonly DeclaredDigestFile FileTwo = new DeclaredDigestFile(
            new byte[] { 1, 1, 1 },
            MessageDigestAlgorithm.SHA256,
            "fileTwo.txt",
            MimeType);

        [Fact(DisplayName = "Verify no digests")]
        public void VerifyNothing()
        {
            var digestVerifier = DigestVerifier.Create(ImmutableDictionary<string, DeclaredDigestFile>.Empty);
            var verification = digestVerifier.Verification();
            verification.ShouldNotBeNull();
            verification.AllValid.ShouldBeTrue();
        }

        [Fact(DisplayName = "Verify digest, multiple declared none received")]
        public void VerifyNoneChecked()
        {
            var digestVerifier = CreateDigestVerifierForTest();

            Func<DigestVerificationResult> action = () => digestVerifier.Verification();

            action.ShouldThrow<DigestVerificationException>().Message.ShouldBe("Total number of files processed by the verifier was 0, but 2 files was declared");
        }

        [Fact(DisplayName = "Verify digest, two declared but only one checked")]
        public void VerifyOneCheckedTwoDeclared()
        {
            var digestVerifier = CreateDigestVerifierForTest();
            digestVerifier.ReceiveDigest(FileOne.Name, FileOne.Digest.ToArray());
            Func<DigestVerificationResult> action = () => digestVerifier.Verification();
            action.ShouldThrow<DigestVerificationException>().Message.ShouldBe("Total number of files processed by the verifier was 1, but 2 files was declared");
        }

        [Fact(DisplayName = "Verify digests, two declared and two checked but with non-matching digests")]
        public void VerifyAllButWrongDigest()
        {
            var digestVerifier = CreateDigestVerifierForTest();
            digestVerifier.ReceiveDigest(FileOne.Name, FileTwo.Digest.ToArray());
            digestVerifier.ReceiveDigest(FileTwo.Name, FileOne.Digest.ToArray());
            var result = digestVerifier.Verification();
            result.AllValid.ShouldBeFalse();
            result.InvalidElements.Count().ShouldBe(2);
        }

        [Fact(DisplayName = "Verify digests, all checked and valid")]
        public void VerifyAllSucceed()
        {
            var digestVerifier = CreateDigestVerifierForTest();
            digestVerifier.ReceiveDigest(FileOne.Name, FileOne.Digest.ToArray());
            digestVerifier.ReceiveDigest(FileTwo.Name, FileTwo.Digest.ToArray());
            var result = digestVerifier.Verification();
            result.AllValid.ShouldBeTrue();
            result.ValidElements.Count().ShouldBe(2);
            result.ValidElements.ShouldContain(FileOne.Name, FileTwo.Name);
        }

        private static DigestVerifier CreateDigestVerifierForTest()
        {
            var declaredDigests = ImmutableList.Create(FileOne, FileTwo).ToImmutableDictionary(d => d.Name, d => d);
            return DigestVerifier.Create(declaredDigests);
        }
    }
}