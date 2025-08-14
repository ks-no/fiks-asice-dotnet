using System;
using System.IO;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;

namespace KS.Fiks.ASiC_E;

public sealed class AsiceBuilder : IAsiceBuilder<AsiceArchive>
{
    private readonly AsiceArchive _asiceArchive;

    private static IManifestCreator FindManifestCreator(
        ManifestSpec spec)
    => spec switch
        {
            // TODO: A standard XAdES manifest has not been implemented
            // yet, since the initial usecase for XAdES required a
            // custom manifest format
            ManifestSpec.Cades => new CadesManifestCreator(),
            _ => throw new ArgumentException(
                "Only CAdES-style manifests are currently supported."),
        };

    private static ISignatureFileRefCreator FindSignatureFileRefCreator(
        ManifestSpec spec,
        ICertificateHolder certificateHolder)
    => (certificateHolder == null)
        ? null
        : spec switch
        {
            ManifestSpec.Cades => new CadesSignature(),
            ManifestSpec.Xades => new XadesSignature(),
            _ => throw new ArgumentException(
                "Only CAdES-style manifests are currently supported."),
        };

    private static ISignatureCreator FindSignatureCreator(
      ManifestSpec spec,
      ICertificateHolder certificateHolder)
    => (certificateHolder == null)
        ? null
        : spec switch
        {
            ManifestSpec.Cades => SignatureCreator.Create(certificateHolder),
            ManifestSpec.Xades => XadesSignatureCreator.Create(certificateHolder),
            _ => throw new ArgumentException(
                "Only CAdES-style manifests are currently supported."),
        };

    private AsiceBuilder(AsiceArchive asiceArchive)
    {
        _asiceArchive = asiceArchive;
    }

    /// <summary>
    /// Create builder
    /// </summary>
    /// <param name="stream">The stream where the ASiC-E data will be written. Can not be null and must be writable</param>
    /// <param name="messageDigestAlgorithm">The digest algorithm to use. Not nullable</param>
    /// <param name="signCertificate">A private/public keypair to use for signing. May be null</param>
    /// <returns>A builder that may be used to construct a ASiC-E package</returns>
    /// <exception cref="ArgumentException">If the provided stream is not writable</exception>
    [Obsolete("This overload is kept for backwards compatibility. Move over to the new Create() that exposes a ManifestSpec parameter.")]
    public static AsiceBuilder Create(
        Stream stream,
        MessageDigestAlgorithm messageDigestAlgorithm,
        ICertificateHolder signCertificate)
    {
        return Create(
            stream,
            messageDigestAlgorithm,
            ManifestSpec.Cades,
            signCertificate);
    }

    /// <summary>
    /// Create builder
    /// </summary>
    /// <param name="stream">The stream where the ASiC-E data will be written. Can not be null and must be writable</param>
    /// <param name="messageDigestAlgorithm">The digest algorithm to use. Not nullable</param>
    /// <param name="manifestSpec">An enum saying the type of signature to add</param>
    /// <param name="signCertificate">A private/public keypair to use for signing. May be null</param>
    /// <returns>A builder that may be used to construct a ASiC-E package</returns>
    /// <exception cref="ArgumentException">If the provided stream is not writable</exception>
    public static AsiceBuilder Create(
        Stream stream,
        MessageDigestAlgorithm messageDigestAlgorithm,
        ManifestSpec manifestSpec,
        ICertificateHolder signCertificate)
    {
        var outStream = stream ?? throw new ArgumentNullException(nameof(stream));
        var algorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        if (!outStream.CanWrite)
        {
            throw new ArgumentException("The provided Stream must be writable", nameof(stream));
        }

        var sigCreator = FindSignatureCreator(manifestSpec, signCertificate);
        var sigFileRefCreator = FindSignatureFileRefCreator(manifestSpec, signCertificate);
        var manifestCreator = FindManifestCreator(manifestSpec);

        return new AsiceBuilder(new AsiceArchive(
            outStream,
            manifestCreator,
            sigFileRefCreator,
            sigCreator,
            signCertificate));
    }

    public AsiceArchive Build()
    {
        return _asiceArchive;
    }

    public IAsiceBuilder<AsiceArchive> AddFile(FileStream file)
    {
        return AddFile(file, file.Name);
    }

    public IAsiceBuilder<AsiceArchive> AddFile(Stream stream, string filename)
    {
        return AddFile(stream, filename, MimeTypeExtractor.ExtractMimeType(filename));
    }

    public IAsiceBuilder<AsiceArchive> AddFile(Stream stream, string filename, MimeType mimeType)
    {
        _asiceArchive.AddEntry(stream, new FileRef(Path.GetFileName(filename), mimeType));
        return this;
    }


    public IAsiceBuilder<AsiceArchive> AddFileWithPath(Stream stream, string filename, MimeType mimeType)
    {
        _asiceArchive.AddEntry(stream, new FileRef(filename, mimeType));
        return this;
    }

    public void Dispose()
    {
        _asiceArchive.Dispose();
    }
}