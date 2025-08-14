using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Sign;
    using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Security;

namespace KS.Fiks.ASiC_E.Model;

public class AsiceArchive : IDisposable
{
    private readonly ZipArchive _zipArchive;

    private readonly IManifestCreator _manifestCreator;

    private readonly ISignatureFileRefCreator _signatureFileRefCreator;

    private readonly MessageDigestAlgorithm _messageDigestAlgorithm;

    private readonly ICertificateHolder _signatureCertificate;

    private readonly Queue<AsicePackageEntry> _entries = new Queue<AsicePackageEntry>();

    private readonly ILogger<AsiceArchive> _logger;

    public AsiceArchive(
        ZipArchive zipArchive,
        IManifestCreator creator,
        ISignatureFileRefCreator signatureFileRefCreator,
        MessageDigestAlgorithm messageDigestAlgorithm,
        ICertificateHolder signatureCertificate,
        ILoggerFactory loggerFactory = null)
    {
        _zipArchive = zipArchive ?? throw new ArgumentNullException(nameof(zipArchive));
        _manifestCreator = creator ?? throw new ArgumentNullException(nameof(creator));
        _messageDigestAlgorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));

        // The certificate holder and the signature file ref creator need to be
        // null at the same time and non-null at the same time:
        if ((signatureCertificate == null) != (signatureFileRefCreator== null))
        {
            var nullArgName = signatureCertificate == null
                ? nameof(signatureCertificate)
                : nameof(signatureFileRefCreator);

            throw new ArgumentNullException(
                nullArgName,
                $"{nameof(signatureCertificate)} must be null if and only if {nameof(signatureFileRefCreator)} is null");
        }

        _signatureCertificate = signatureCertificate;
        _signatureFileRefCreator = signatureFileRefCreator;

        _logger = loggerFactory?.CreateLogger<AsiceArchive>() ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AsiceArchive>();
        _logger.LogDebug("Creating ASiC-e Zip");
    }

    public AsiceArchive(
        Stream zipOutStream,
        IManifestCreator creator,
        ISignatureFileRefCreator signatureFileRefCreator,
        ICertificateHolder signatureCertificateHolder,
        ILoggerFactory loggerFactory = null)
    {
        if (zipOutStream == null)
        {
            throw new ArgumentNullException(nameof(zipOutStream));
        }

        _zipArchive = new ZipArchive(zipOutStream, ZipArchiveMode.Create, true, Encoding.UTF8);
        var zipArchiveEntry = _zipArchive.CreateEntry(AsiceConstants.FileNameMimeType);

        using var stream = zipArchiveEntry.Open();
        var contentAsBytes = Encoding.UTF8.GetBytes(AsiceConstants.ContentTypeASiCe);
        stream.Write(contentAsBytes, 0, contentAsBytes.Length);

        _manifestCreator = creator ?? throw new ArgumentNullException(nameof(creator));
        _messageDigestAlgorithm = MessageDigestAlgorithm.SHA256;

        // The certificate holder and the signature file ref creator need to be
        // null at the same time and non-null at the same time:
        if ((signatureCertificateHolder == null) != (signatureFileRefCreator== null))
        {
            var nullArgName = signatureCertificateHolder == null
                ? nameof(signatureCertificateHolder)
                : nameof(signatureFileRefCreator);

            throw new ArgumentNullException(
                nullArgName,
                $"{nameof(signatureCertificateHolder)} must be null if and only if {nameof(signatureFileRefCreator)} is null");
        }

        _signatureCertificate = signatureCertificateHolder;
        _signatureFileRefCreator = signatureFileRefCreator;

        _logger = loggerFactory?.CreateLogger<AsiceArchive>() ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AsiceArchive>();
        _logger.LogDebug("Creating ASiC-e Zip");
    }

    /// <summary>
    /// Add file to ASiC-E package
    /// </summary>
    /// <param name="contentStream">The stream that contains the data</param>
    /// <param name="entry">A description of the file entry</param>
    /// <returns>The archive with the entry added</returns>
    /// <exception cref="ArgumentException">If any of the parameters is null or invalid.
    /// Only files that are not in the /META-INF may be added</exception>
    public AsiceArchive AddEntry(Stream contentStream, FileRef entry)
    {
        var packageEntry = entry ?? throw new ArgumentNullException(nameof(entry), "Entry must be provided");
        if (packageEntry.FileName.StartsWith("META-INF/", StringComparison.CurrentCultureIgnoreCase))
        {
            throw new ArgumentException("Adding files to META-INF is not allowed.");
        }

        _logger.LogDebug("Adding entry '{FileName}' of type '{MimeType}' to the ASiC-e archive", entry.FileName,
            entry.MimeType);

        _entries.Enqueue(CreateEntry(contentStream,
            new AsicePackageEntry(entry.FileName, entry.MimeType, _messageDigestAlgorithm)));
        return this;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool dispose)
    {
        var signatureFileRef = _signatureFileRefCreator?.CreateSignatureRef();
        AddManifest(signatureFileRef);
        _zipArchive.Dispose();
    }

    private AsicePackageEntry CreateEntry(Stream contentStream, AsicePackageEntry entry)
    {
        var fileName = entry.FileName ?? throw new ArgumentNullException(nameof(entry), "File name must be provided");
        var dataStream = contentStream ?? throw new ArgumentNullException(nameof(contentStream));
        var zipEntry = _zipArchive.CreateEntry(fileName);
        using var zipStream = zipEntry.Open();
        using var digestStream = new DigestStream(zipStream, null, _messageDigestAlgorithm.Digest);
        dataStream.CopyTo(digestStream);
        dataStream.Flush();
        entry.Digest = new DigestContainer(DigestUtilities.DoFinal(digestStream.WriteDigest), _messageDigestAlgorithm);

        return entry;
    }

    private void AddManifest(SignatureFileRef signatureFileRef)
    {
        _logger.LogDebug("Creating manifest");
        var manifest =  _manifestCreator.CreateManifest(_entries, signatureFileRef);
        if (manifest.ManifestSpec == ManifestSpec.Cades && signatureFileRef != null)
        {
            manifest.SignatureFileRef = signatureFileRef;
            var signatureFile = SignatureCreator.Create(_signatureCertificate).CreateCadesSignatureFile(manifest);
            using var signatureStream = new MemoryStream(signatureFile.Data.ToArray());
            var entry = _zipArchive.CreateEntry(signatureFile.SignatureFileRef.FileName);
            using var zipEntryStream = entry.Open();
            signatureStream.CopyTo(zipEntryStream);
        }

        using (var manifestStream = new MemoryStream(manifest.Data.ToArray()))
        {
            CreateEntry(manifestStream,
                new AsicePackageEntry(manifest.FileName, MimeType.ForString(AsiceConstants.ContentTypeXml),
                    _messageDigestAlgorithm));
        }

        _logger.LogDebug("Manifest added to archive");
    }
}