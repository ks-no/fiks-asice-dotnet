using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
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

    private static readonly Regex CadesManifestPattern = new Regex(
        @"^META-INF/ASiCManifest[^/]*\.xml\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly ZipArchive _zipArchive;

    private readonly byte[] _cadesManifestData;

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
        var cadesManifestEntry = FindCadesManifestEntry(asicArchive);
        _cadesManifestData = cadesManifestEntry == null ? null : ReadEntry(cadesManifestEntry);

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
                $"Archive is not a valid ASiC-E archive as the entry '{invalidPath.FullName}' has an invalid path",
                nameof(zipArchive));
        }

        // FileName is only the name of the file, so files with the same name in different folders
        // could overwrite each other if a receiver uses FileName as a key. Names that differ only in
        // case are allowed in the root folder, as they always have been, but not when folders are involved
        var duplicateFileName = GetDataEntries(zipArchive)
            .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(IsFileNameConflict);
        if (duplicateFileName != null)
        {
            throw new ArgumentException(
                $"Archive is not supported as more than one file is named '{duplicateFileName.Key}'",
                nameof(zipArchive));
        }
    }

    private static bool IsFileNameConflict(IGrouping<string, ZipArchiveEntry> entriesWithSameName)
    {
        var entries = entriesWithSameName.ToList();
        return entries.Count > 1
               && (entries.Any(entry => entry.FullName != entry.Name)
                   || entries.Select(entry => entry.Name).Distinct(StringComparer.Ordinal).Count() < entries.Count);
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
                $"Could not find declared digest method for entry '{fullEntryName}'");
        }

        return declaredDigest.MessageDigestAlgorithm;
    }

    // Only one manifest is supported. A package with more than one is rejected, so that an extra
    // unsigned manifest can not be picked instead of the signed one
    private static ZipArchiveEntry FindCadesManifestEntry(ZipArchive zipArchive)
    {
        var manifests = zipArchive.Entries.Where(entry => CadesManifestPattern.IsMatch(entry.FullName)).ToList();
        if (manifests.Count > 1)
        {
            throw new ArgumentException(
                $"Archive is not supported as it has more than one manifest: {string.Join(",", manifests.Select(m => m.FullName))}",
                nameof(zipArchive));
        }

        return manifests.SingleOrDefault();
    }

    private byte[] GetCadesManifestBlob()
    {
        return _cadesManifestData;
    }

    // Parsed from the same bytes as the signature is checked against
    private CadesManifest GetCadesManifest()
    {
        if (_cadesManifestData == null)
        {
            return null;
        }

        using var manifestStream = new MemoryStream(_cadesManifestData);
        return new CadesManifestReader().FromStream(manifestStream);
    }

    private ZipArchiveEntry GetEntry(string fullEntryName)
    {
        return _zipArchive.Entries.SingleOrDefault(entry =>
            entry.FullName.Equals(fullEntryName, StringComparison.CurrentCultureIgnoreCase));
    }

    private byte[] GetDataForEntry(string fullEntryName)
    {
        var zipArchiveEntry = GetEntry(fullEntryName);
        return zipArchiveEntry == null ? null : ReadEntry(zipArchiveEntry);
    }

    private static byte[] ReadEntry(ZipArchiveEntry zipArchiveEntry)
    {
        using var zipStream = zipArchiveEntry.Open();
        using var bufferStream = new MemoryStream();
        zipStream.CopyTo(bufferStream);
        return bufferStream.ToArray();
    }

    private Signatures ExtractSignaturesFromManifest()
    {
        var signatures = Manifest?.GetSignatureRefs()
            .Select(s => new SignatureFileContainer(
                s,
                GetDataForEntry(s.FileName) ?? throw new SignatureVerificationException(
                    $"The signature file '{s.FileName}' referenced in the manifest is missing")));
        return signatures == null ? null : new Signatures(signatures);
    }
}