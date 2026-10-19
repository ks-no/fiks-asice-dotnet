using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Sign;
using KS.Fiks.ASiC_E.Xsd;
using Microsoft.Extensions.Logging;

namespace KS.Fiks.ASiC_E.Model;

public sealed class AsiceReadModel : IDisposable
{
    public IEnumerable<AsiceReadEntry> Entries { get; }

    public CadesManifest CadesManifest { get; }

    public Signatures Signatures { get; }

    public IDigestVerifier DigestVerifier => _digestVerifier;

    private AbstractManifest Manifest => CadesManifest;

    private readonly ZipArchive _zipArchive;

    private readonly DigestVerifier _digestVerifier;

    private readonly ILoggerFactory _loggerFactory;

    public AsiceReadModel(ZipArchive zipArchive, ILoggerFactory loggerFactory = null)
    {
        _loggerFactory = loggerFactory;

        var asicArchive = zipArchive ?? throw new ArgumentNullException(nameof(zipArchive));
        if (zipArchive.Mode != ZipArchiveMode.Read)
        {
            throw new ArgumentException("The provided ZipArchive should be in READ mode", nameof(zipArchive));
        }

        var firstEntry = asicArchive.Entries.FirstOrDefault();
        if (firstEntry == null || firstEntry.FullName != AsiceConstants.FileNameMimeType)
        {
            throw new ArgumentException(
                $"Archive is not a valid ASiC-E archive as the first entry is not '{AsiceConstants.FileNameMimeType}'",
                nameof(zipArchive));
        }

        ValidateEntryNames(asicArchive);

        _zipArchive = zipArchive;

        CadesManifest = GetCadesManifest();
        Entries = GetAsiceEntries();
        Signatures = ExtractSignaturesFromManifest();

        var declaredDigests = Manifest?.GetDeclaredDigests();
        if (declaredDigests != null)
        {
            _digestVerifier = Crypto.DigestVerifier.Create(declaredDigests, _loggerFactory);
        }
    }

    /// <summary>
    /// Verifies the CAdES signature over the manifest and returns the content of the manifest.
    /// Only the signature is checked here. To check the digests of the files, read every entry and call
    /// <see cref="IDigestVerifier.Verification"/>, or use <see cref="AsiceVerifier"/> which does both.
    /// <c>verified</c> on each file means that the file is listed in a manifest with a valid signature,
    /// not that its digest has been checked. The trust chain of the signing certificate is not checked.
    /// </summary>
    /// <returns>The manifest, or null if the package has no CAdES manifest or no signature</returns>
    /// <exception cref="SignatureVerificationException">If the signature is invalid</exception>
    public asicManifest VerifiedManifest()
    {
        var signature = Signatures?.Containers?.FirstOrDefault();
        if (signature != null && CadesManifest != null)
        {
            var certificate =
                new SignatureVerifier(_loggerFactory).Validate(
                    GetCadesManifestBlob(),
                    signature.Data.ToArray());
            return new asicManifest
            {
                certificate = new[] { certificate },
                file = CadesManifest.Digests.Select(d => new asicFile
                {
                    digest = d.Value.Digest.ToArray(),
                    mimetype = d.Value.MimeType.ToString(),
                    name = d.Value.Name,
                    verified = true
                }).ToArray(),
                rootfile = CadesManifest.RootFile
            };
        }

        return null;
    }

    public void Dispose()
    {
        _zipArchive.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ValidateEntryNames(ZipArchive zipArchive)
    {
        var invalidPath = zipArchive.Entries.FirstOrDefault(entry => !EntryPath.IsValid(entry.FullName));
        if (invalidPath != null)
        {
            throw new ArgumentException(
                $"Archive is not a valid ASiC-E archive as the entry '{EntryPath.Escape(invalidPath.FullName)}' has an invalid path. It must be relative, with '/' as separator and without empty, '.' or '..' segments",
                nameof(zipArchive));
        }

        // FileName is only the name of the file, so files with the same name in different folders
        // could overwrite each other if a receiver uses FileName as a key
        var duplicateFileName = GetDataEntries(zipArchive)
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(entries => EntryPath.IsFileNameConflict(entries.Select(entry => entry.FullName).ToList()));
        if (duplicateFileName != null)
        {
            throw new ArgumentException(
                $"Archive is not supported as more than one file is named '{EntryPath.Escape(duplicateFileName.Key)}'",
                nameof(zipArchive));
        }
    }

    private static IEnumerable<ZipArchiveEntry> GetDataEntries(ZipArchive zipArchive)
    {
        return zipArchive.Entries
            .Where(entry => entry.FullName != AsiceConstants.FileNameMimeType)
            .Where(entry => !entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase))
            .Where(entry => !EntryPath.IsFolder(entry.FullName));
    }

    private IEnumerable<AsiceReadEntry> GetAsiceEntries()
    {
        return GetDataEntries(_zipArchive)
            .Select(entry => new AsiceReadEntry(
                entry,
                LookupMessageDigestAlgorithmForEntry(entry.FullName),
                _digestVerifier));
    }

    private MessageDigestAlgorithm LookupMessageDigestAlgorithmForEntry(string fullEntryName)
    {
        DeclaredDigestFile declaredDigest = null;
        if (CadesManifest?.Digests?.TryGetValue(fullEntryName, out declaredDigest) != true)
        {
            throw new DigestVerificationException(
                $"Could not find declared digest method for entry '{EntryPath.Escape(fullEntryName)}'");
        }

        return declaredDigest.MessageDigestAlgorithm;
    }

    private byte[] GetCadesManifestBlob()
    {
        return GetDataForEntry(AsiceConstants.CadesManifestFilename);
    }

    private CadesManifest GetCadesManifest()
    {
        var cadesManifestEntry = GetCadesManifestEntry();
        if (cadesManifestEntry == null)
        {
            return null;
        }

        using var entryStream = cadesManifestEntry.Open();
        return new CadesManifestReader().FromStream(entryStream);
    }

    private ZipArchiveEntry GetCadesManifestEntry()
    {
        return GetEntry(AsiceConstants.CadesManifestFilename);
    }

    private ZipArchiveEntry GetEntry(string fullEntryName)
    {
        return _zipArchive.Entries.SingleOrDefault(entry =>
            entry.FullName.Equals(fullEntryName, StringComparison.CurrentCultureIgnoreCase));
    }

    private byte[] GetDataForEntry(string fullEntryName)
    {
        var zipArchiveEntry = GetEntry(fullEntryName);
        if (zipArchiveEntry == null)
        {
            return null;
        }

        using var zipStream = zipArchiveEntry.Open();
        using var bufferStream = new MemoryStream();
        zipStream.CopyTo(bufferStream);
        return bufferStream.ToArray();
    }

    private Signatures ExtractSignaturesFromManifest()
    {
        var signatures = Manifest?.GetSignatureRefs()
            .Select(s => new SignatureFileContainer(s, GetSignatureData(s.FileName)));
        return signatures == null ? null : new Signatures(signatures);
    }

    private byte[] GetSignatureData(string fileName)
    {
        return GetDataForEntry(fileName) ?? throw new SignatureVerificationException(
            $"The signature file '{EntryPath.Escape(fileName)}' referenced in the manifest is missing");
    }
}