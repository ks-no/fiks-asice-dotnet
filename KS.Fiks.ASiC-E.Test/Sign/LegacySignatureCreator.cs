using System.Collections.Generic;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Operators;

namespace KS.Fiks.ASiC_E.Test.Sign;

/// <summary>
/// Creates signatures the way KS.Fiks.ASiC-E 4.0.x did, with the same signer added twice
/// </summary>
public class LegacySignatureCreator : ISignatureCreator
{
    private readonly ICertificateHolder _certificateHolder;

    public LegacySignatureCreator(ICertificateHolder certificateHolder)
    {
        _certificateHolder = certificateHolder;
    }

    public static byte[] Sign(ICertificateHolder certificateHolder, byte[] data)
    {
        var signedDataGenerator = new CmsSignedDataGenerator();
        signedDataGenerator.AddSigner(
            certificateHolder.GetPrivateKey(),
            certificateHolder.GetPublicCertificate(),
            CmsSignedGenerator.DigestSha256);
        signedDataGenerator.AddSignerInfoGenerator(new SignerInfoGeneratorBuilder().Build(
            new Asn1SignatureFactory(AsiceConstants.SignatureAlgorithm, certificateHolder.GetPrivateKey()),
            certificateHolder.GetPublicCertificate()));
        signedDataGenerator.AddCertificate(certificateHolder.GetPublicCertificate());
        return signedDataGenerator.Generate(new CmsProcessableByteArray(data)).GetEncoded();
    }

    public SignatureFileContainer CreateSignatureFile(
        ManifestContainer manifestContainer,
        IEnumerable<AsicePackageEntry> asicPackageEntries)
    {
        return new SignatureFileContainer(
            manifestContainer.SignatureFileRef,
            Sign(_certificateHolder, manifestContainer.Data.ToArray()));
    }
}
