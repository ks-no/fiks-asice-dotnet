using System;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Sign
{
    public class SignatureVerifierTest
    {
        private static readonly byte[] Manifest = Encoding.UTF8.GetBytes("<ASiCManifest/>");

        [Fact(DisplayName = "Verify signature")]
        public void VerifySignature()
        {
            var signature = TestdataLoader.ReadFromResource("signature.p7s");
            var signedData = TestdataLoader.ReadFromResource("signedData.xml");
            var signatureVerifier = new SignatureVerifier();
            var certificate = signatureVerifier.Validate(signedData, signature);
            certificate.ShouldNotBeNull();
            certificate.subject.ShouldContain("DIREKTORATET FOR FORVALTNING OG IKT");
        }

        [Fact(DisplayName = "Signature over changed data is rejected")]
        public void ChangedDataIsRejected()
        {
            var signature = TestdataLoader.ReadFromResource("signature.p7s");
            var signedData = TestdataLoader.ReadFromResource("signedData.xml");
            signedData[signedData.Length / 2] ^= 1;

            Action validate = () => new SignatureVerifier().Validate(signedData, signature);

            validate.ShouldThrow<SignatureVerificationException>();
        }

        [Fact(DisplayName = "Signature created by the library verifies and has exactly one signer")]
        public void CreatedSignatureHasOneSigner()
        {
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            var signature = CreateSignature(certificateHolder, Manifest);

            new CmsSignedData(new CmsProcessableByteArray(Manifest), signature)
                .GetSignerInfos().Count.ShouldBe(1);

            var certificate = new SignatureVerifier().Validate(Manifest, signature);
            certificate.certificate1.ShouldBe(certificateHolder.GetPublicCertificate().GetEncoded());
        }

        [Fact(DisplayName = "Signature with two signers, as created by 4.0.x, verifies")]
        public void LegacySignatureWithTwoSignersVerifies()
        {
            var signature = LegacySignatureCreator.Sign(TestdataLoader.ReadCertificatesForTest(), Manifest);

            new CmsSignedData(new CmsProcessableByteArray(Manifest), signature)
                .GetSignerInfos().Count.ShouldBe(2);

            new SignatureVerifier().Validate(Manifest, signature).ShouldNotBeNull();
        }

        [Fact(DisplayName = "Signature where one of two signers is invalid is rejected")]
        public void OneInvalidSignerIsRejected()
        {
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            var generator = new CmsSignedDataGenerator();
            generator.AddSignerInfoGenerator(CreateSignerInfoGenerator(certificateHolder));

            // Signed with another key than the one in the certificate
            var otherKey = GenerateRsaKeyPair().Private;
            generator.AddSignerInfoGenerator(new SignerInfoGeneratorBuilder().Build(
                new Asn1SignatureFactory(AsiceConstants.SignatureAlgorithm, otherKey),
                certificateHolder.GetPublicCertificate()));
            generator.AddCertificate(certificateHolder.GetPublicCertificate());
            var signature = generator.Generate(new CmsProcessableByteArray(Manifest)).GetEncoded();

            Action validate = () => new SignatureVerifier().Validate(Manifest, signature);

            validate.ShouldThrow<SignatureVerificationException>();
        }

        [Fact(DisplayName = "Signature that carries other content than the data is rejected")]
        public void AttachedOtherContentIsRejected()
        {
            var generator = new CmsSignedDataGenerator();
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            generator.AddSignerInfoGenerator(CreateSignerInfoGenerator(certificateHolder));
            generator.AddCertificate(certificateHolder.GetPublicCertificate());
            var signature = generator
                .Generate(new CmsProcessableByteArray(Encoding.UTF8.GetBytes("<OtherManifest/>")), true)
                .GetEncoded();

            Action validate = () => new SignatureVerifier().Validate(Manifest, signature);

            validate.ShouldThrow<SignatureVerificationException>();
        }

        [Fact(DisplayName = "Signature without signers is rejected")]
        public void NoSignersIsRejected()
        {
            var certificateHolder = TestdataLoader.ReadCertificatesForTest();
            var generator = new CmsSignedDataGenerator();
            generator.AddCertificate(certificateHolder.GetPublicCertificate());
            var signature = generator.Generate(new CmsProcessableByteArray(Manifest)).GetEncoded();

            Action validate = () => new SignatureVerifier().Validate(Manifest, signature);

            validate.ShouldThrow<SignatureVerificationException>().Message.ShouldBe("The signature has no signers");
        }

        [Fact(DisplayName = "Signature without the certificate of the signer is rejected")]
        public void MissingCertificateIsRejected()
        {
            var generator = new CmsSignedDataGenerator();
            generator.AddSignerInfoGenerator(CreateSignerInfoGenerator(TestdataLoader.ReadCertificatesForTest()));
            var signature = generator.Generate(new CmsProcessableByteArray(Manifest)).GetEncoded();

            Action validate = () => new SignatureVerifier().Validate(Manifest, signature);

            validate.ShouldThrow<SignatureVerificationException>().Message
                .ShouldBe("The signature does not contain the certificate of the signer");
        }

        [Fact(DisplayName = "Signature that is not CMS is rejected")]
        public void GarbageIsRejected()
        {
            Action validate = () => new SignatureVerifier().Validate(Manifest, Encoding.UTF8.GetBytes("not a signature"));

            validate.ShouldThrow<SignatureVerificationException>();
        }

        private static byte[] CreateSignature(ICertificateHolder certificateHolder, byte[] data)
        {
            var manifest = new ManifestContainer(
                AsiceConstants.CadesManifestFilename,
                data,
                new SignatureFileRef("META-INF/signature.p7s"),
                ManifestSpec.Cades);
            return SignatureCreator.Create(certificateHolder)
                .CreateSignatureFile(manifest, Enumerable.Empty<AsicePackageEntry>())
                .Data.ToArray();
        }

        private static SignerInfoGenerator CreateSignerInfoGenerator(ICertificateHolder certificateHolder)
        {
            return new SignerInfoGeneratorBuilder().Build(
                new Asn1SignatureFactory(AsiceConstants.SignatureAlgorithm, certificateHolder.GetPrivateKey()),
                certificateHolder.GetPublicCertificate());
        }

        private static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair GenerateRsaKeyPair()
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(65537), new SecureRandom(), 2048, 80));
            return generator.GenerateKeyPair();
        }
    }
}
