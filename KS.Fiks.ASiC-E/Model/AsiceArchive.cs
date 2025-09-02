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

    private readonly ISignatureCreator _signatureCreator;

    private readonly ISignatureFileRefCreator _signatureFileRefCreator;

    private readonly MessageDigestAlgorithm _messageDigestAlgorithm;

    private readonly Queue<AsicePackageEntry> _entries = new Queue<AsicePackageEntry>();

    private readonly ILogger<AsiceArchive> _logger;

    public AsiceArchive(
        ZipArchive zipArchive,
        IManifestCreator creator,
        ISignatureFileRefCreator signatureFileRefCreator,
        MessageDigestAlgorithm messageDigestAlgorithm,
        ISignatureCreator signatureCreator,
        ILoggerFactory loggerFactory = null)
    {
        _zipArchive = zipArchive ?? throw new ArgumentNullException(nameof(zipArchive));
        _manifestCreator = creator ?? throw new ArgumentNullException(nameof(creator));
        _messageDigestAlgorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));

        _signatureFileRefCreator = signatureFileRefCreator;
        _signatureCreator = signatureCreator;

        _logger = loggerFactory?.CreateLogger<AsiceArchive>() ?? LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AsiceArchive>();
        _logger.LogDebug("Creating ASiC-e Zip");
    }

    public AsiceArchive(
        Stream zipOutStream,
        IManifestCreator creator,
        ISignatureFileRefCreator signatureFileRefCreator,
        ISignatureCreator signatureCreator,
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

        _signatureFileRefCreator = signatureFileRefCreator;
        _signatureCreator = signatureCreator;

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
        // In cases where both a manifest and a signature should be added,
        // there is a seeming circular dependency:
        //
        // * The manifest needs to know about the signature.
        // * The signature needs to know about the manifest.
        //
        // However, the manifest only needs to know about the location of the
        // signature, not its contents; while the signature needs to know
        // about the contents of the manifest in order to hash that manifest
        // in order to include it. Therefore, the manifest is created before
        // the signature, but the *location* of the signature is determined
        // before either of them.
        //
        // If signatureFileRef is null, it means no signature should be
        // created, which is handled inside AddSignature().
        var signatureFileRef = _signatureFileRefCreator?.CreateSignatureRef();
        var manifestContainer = AddManifest(signatureFileRef);
        AddSignature(manifestContainer);
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

    private ManifestContainer AddManifest(SignatureFileRef signatureFileRef)
    {
        _logger.LogDebug("Creating manifest");
        var manifest = _manifestCreator.CreateManifest(_entries, signatureFileRef);

        // TODO: Add a MimeType property on ManifestContainer instead, so that
        // the following call doesn't need to assume that all manifests will
        // be XML always. For now, though, this works.
        var manifestPkgEntry = new AsicePackageEntry(
            manifest.FileName,
            MimeType.ForString(AsiceConstants.ContentTypeApplicationXml),
            _messageDigestAlgorithm);

        manifest.PackageEntry = manifestPkgEntry;

        using (var manifestStream = new MemoryStream(manifest.Data.ToArray()))
        {
            CreateEntry(manifestStream, manifestPkgEntry);
        }

        _logger.LogDebug("Manifest added to archive");
        return manifest;
    }

    private void AddSignature(ManifestContainer manifest)
    {
        if (manifest.SignatureFileRef == null)
        {
            return;
        }

        var sigFileContainer = _signatureCreator?.CreateSignatureFile(
            manifest,
            _entries);

        if (sigFileContainer == null)
        {
            return;
        }

        using var signatureStream = new MemoryStream(sigFileContainer.Data.ToArray());
        var entry = _zipArchive.CreateEntry(sigFileContainer.SignatureFileRef.FileName);
        using var zipEntryStream = entry.Open();
        signatureStream.CopyTo(zipEntryStream);
    }
}
