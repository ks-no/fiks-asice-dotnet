using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Sign;
using KS.Fiks.ASiC_E.Xsd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KS.Fiks.ASiC_E.Model;

public sealed class AsiceReadModel : IDisposable
{
    private const int MaxNamesInWarning = 10;

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

        ValidateEntryNames(asicArchive, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AsiceReadModel>());

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

    private static void ValidateEntryNames(ZipArchive zipArchive, ILogger logger)
    {
        var notPortable = new List<string>();
        foreach (var entry in zipArchive.Entries)
        {
            var rule = AsiceEntryPath.FirstViolation(entry.FullName);
            if (rule == null)
            {
                continue;
            }

            if (!AsiceEntryPath.IsPortabilityRule(rule.Value))
            {
                throw new ArgumentException(
                    $"Archive is not a valid ASiC-E archive as the entry '{AsiceEntryPath.Escape(entry.FullName)}' has an invalid path: {AsiceEntryPath.Describe(entry.FullName)}",
                    nameof(zipArchive));
            }

            notPortable.Add($"'{AsiceEntryPath.Escape(entry.FullName)}' ({AsiceEntryPath.Describe(entry.FullName)})");
        }

        // Names that are legal on some systems but not on all are accepted, so that packages from other
        // senders can still be read. The receiver must check the names before writing the files to disk
        if (notPortable.Count > 0)
        {
            logger.LogWarning(
                "{Count} entries have names that are not portable. Check them before you use them as paths on disk: {Names}",
                notPortable.Count,
                string.Join(", ", notPortable.Take(MaxNamesInWarning)) + (notPortable.Count > MaxNamesInWarning ? ", ..." : string.Empty));
        }

        RejectFileAndFolderWithSameName(zipArchive);
        CheckDuplicateFileNames(zipArchive, logger);
    }

    private static void RejectFileAndFolderWithSameName(ZipArchive zipArchive)
    {
        // A file cannot be created where a folder is, so such a package cannot be unpacked anywhere. Every file
        // counts, also mimetype and the files in META-INF. In ordinal order the paths in a folder come right
        // after "folder/", so a binary search finds them without keeping every folder prefix, which would take
        // memory quadratic in the depth of a path
        var paths = zipArchive.Entries.Select(entry => entry.FullName).ToList();
        paths.Sort(StringComparer.Ordinal);
        foreach (var file in paths.Where(path => !AsiceEntryPath.IsFolder(path)))
        {
            var folder = file + "/";
            var index = paths.BinarySearch(folder, StringComparer.Ordinal);
            if (index < 0)
            {
                index = ~index;
            }

            if (index < paths.Count && paths[index].StartsWith(folder, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Archive is not supported as '{AsiceEntryPath.Escape(file)}' is both a file and a folder",
                    nameof(zipArchive));
            }
        }
    }

    private static void CheckDuplicateFileNames(ZipArchive zipArchive, ILogger logger)
    {
        // Names that differ only in Unicode normalisation are the same file on macOS, so they count as equal
        var normalizationUnsupported = false;
        string Normalize(string name)
        {
            if (normalizationUnsupported)
            {
                return name;
            }

            try
            {
                return name.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException)
            {
                return name;
            }
            catch (PlatformNotSupportedException)
            {
                normalizationUnsupported = true;
                return name;
            }
        }

        var groups = GetDataEntries(zipArchive)
            .GroupBy(entry => Normalize(entry.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.ToList())
            .Where(entries => entries.Count > 1)
            .ToList();

        if (normalizationUnsupported)
        {
            logger.LogWarning(
                "Unicode normalisation is not supported on this platform, so file names that differ only in normalisation are not detected as duplicates");
        }

        // FileName is only the name of the file, so files with the same name in different folders
        // could overwrite each other if a receiver uses FileName as a key. Names that differ only in
        // case are allowed in the root folder, as they always have been, but not when folders are involved
        var conflict = groups.FirstOrDefault(entries =>
            entries.Any(entry => entry.FullName != entry.Name)
            || entries.Select(entry => Normalize(entry.Name)).Distinct(StringComparer.Ordinal).Count() < entries.Count);
        if (conflict != null)
        {
            throw new ArgumentException(
                $"Archive is not supported as more than one file is named '{AsiceEntryPath.Escape(conflict[0].Name)}'",
                nameof(zipArchive));
        }

        foreach (var entries in groups)
        {
            logger.LogWarning(
                "The files {Names} differ only in case. They are the same file on Windows and macOS",
                string.Join(", ", entries.Select(entry => $"'{AsiceEntryPath.Escape(entry.FullName)}'")));
        }
    }

    private static IEnumerable<ZipArchiveEntry> GetDataEntries(ZipArchive zipArchive)
    {
        return zipArchive.Entries
            .Where(entry => entry.FullName != AsiceConstants.FileNameMimeType)
            .Where(entry => !entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase))
            .Where(entry => !AsiceEntryPath.IsFolder(entry.FullName));
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
                $"Could not find declared digest method for entry '{AsiceEntryPath.Escape(fullEntryName)}'");
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
            $"The signature file '{AsiceEntryPath.Escape(fileName)}' referenced in the manifest is missing");
    }
}