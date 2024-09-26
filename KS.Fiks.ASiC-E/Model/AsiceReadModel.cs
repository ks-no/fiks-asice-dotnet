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

        _zipArchive = zipArchive;

        CadesManifest = GetCadesManifest();
        Entries = GetAsiceEntries();
        Signatures = ExtractSignaturesFromManifest();

        var declaredDigests = Manifest?.GetDeclaredDigests();
        if (declaredDigests != null)
        {
            _digestVerifier = Crypto.DigestVerifier.Create(declaredDigests);
        }
    }

    public asicManifest VerifiedManifest()
    {
        if (Signatures?.Containers?.FirstOrDefault() != null && CadesManifest != null)
        {
            var certificate =
                new SignatureVerifier(_loggerFactory).Validate(
                    GetCadesManifestBlob(),
                    Signatures.Containers.FirstOrDefault().Data.ToArray());
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

    private IEnumerable<AsiceReadEntry> GetAsiceEntries()
    {
        return _zipArchive.Entries.Where(entry => entry.Name != AsiceConstants.FileNameMimeType)
            .Where(entry => !entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase))
            .Select(entry => new AsiceReadEntry(
                entry,
                LookupMessageDigestAlgorithmForEntry(entry.FullName),
                _digestVerifier));
    }

    private MessageDigestAlgorithm LookupMessageDigestAlgorithmForEntry(string fullEntryName)
    {
        var declaredDigest = CadesManifest?.Digests[fullEntryName];
        if (declaredDigest == null)
        {
            throw new DigestVerificationException(
                $"Could not find declared digest method for entry '{fullEntryName}'");
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
            .Select(s => new SignatureFileContainer(s, GetDataForEntry(s.FileName)));
        return signatures == null ? null : new Signatures(signatures);
    }
}