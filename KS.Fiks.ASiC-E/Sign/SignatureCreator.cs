using System;
using System.Collections.Generic;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Operators;

namespace KS.Fiks.ASiC_E.Sign
{
    public class SignatureCreator : ISignatureCreator
    {
        private readonly ICertificateHolder _certificateHolder;

        private SignatureCreator(ICertificateHolder certificateHolder)
        {
            _certificateHolder = certificateHolder ?? throw new ArgumentNullException(nameof(certificateHolder));
        }

        public static SignatureCreator Create(ICertificateHolder certificateHolder)
        {
            return new SignatureCreator(certificateHolder);
        }

        public SignatureFileContainer CreateSignatureFile(IEnumerable<AsicePackageEntry> asicPackageEntries)
        {
            throw new NotImplementedException();
        }

        public SignatureFileContainer CreateCadesSignatureFile(ManifestContainer manifestContainer)
        {
            var signedDataGenerator = new CmsSignedDataGenerator();
            signedDataGenerator.AddSigner(
                _certificateHolder.GetPrivateKey(),
                _certificateHolder.GetPublicCertificate(),
                CmsSignedGenerator.DigestSha256);
            signedDataGenerator.AddSignerInfoGenerator(CreateSignerInfoGenerator());
            signedDataGenerator.AddCertificate(_certificateHolder.GetPublicCertificate());
            var signedData =
                signedDataGenerator.Generate(new CmsProcessableByteArray(manifestContainer.Data.ToArray()));
            return new SignatureFileContainer(manifestContainer.SignatureFileRef, signedData.GetEncoded());
        }

        private SignerInfoGenerator CreateSignerInfoGenerator()
        {
            return new SignerInfoGeneratorBuilder().Build(
                CreateContentSigner(),
                _certificateHolder.GetPublicCertificate());
        }

        private Asn1SignatureFactory CreateContentSigner()
        {
            return new Asn1SignatureFactory(AsiceConstants.SignatureAlgorithm, _certificateHolder.GetPrivateKey());
        }
    }
}