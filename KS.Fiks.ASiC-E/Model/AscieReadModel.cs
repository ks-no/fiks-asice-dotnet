using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Linq;
using KS.Fiks.ASiC_E.Manifest;

namespace KS.Fiks.ASiC_E.Model
{
    public class AscieReadModel : IDisposable
    {
        private readonly ZipArchive _zipArchive;
        private readonly MessageDigestAlgorithm _digestAlgorithm;
        private readonly IDictionary<string, string> _calculatedDigest = ImmutableDictionary.Create<string, string>();

        private AscieReadModel(ZipArchive zipArchive, MessageDigestAlgorithm digestAlgorithm)
        {
            this._zipArchive = zipArchive;
            this._digestAlgorithm = digestAlgorithm;
        }

        public IEnumerable<AsiceReadEntry> Entries =>
            this._zipArchive.Entries.Where(entry => entry.Name != AsiceConstants.FileNameMimeType)
                .Where(entry => ! entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase))
                .Select(entry => new AsiceReadEntry(entry, this._digestAlgorithm));

        public CadesManifest CadesManifest => GetCadesManifest();

        public static AscieReadModel Create(ZipArchive zipArchive, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            var asicArchive = zipArchive ?? throw new ArgumentNullException(nameof(zipArchive));
            var firstEntry = asicArchive.Entries.FirstOrDefault();
            if (firstEntry == null || firstEntry.FullName != AsiceConstants.FileNameMimeType)
            {
                throw new ArgumentException($"Archive is not a valid ASiC-E archive as the first entry is not '{AsiceConstants.FileNameMimeType}'", nameof(zipArchive));
            }

            var digestAlg = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));

            return new AscieReadModel(asicArchive, digestAlg);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            // TODO: verify signature
        }

        private CadesManifest GetCadesManifest()
        {
            var cadesManifestEntry = GetEntry(AsiceConstants.CadesManifestFilename);
            if (cadesManifestEntry == null)
            {
                return null;
            }

            using (var entryStream = cadesManifestEntry.Open())
            {
                return new CadesManifestReader().FromStream(entryStream);
            }
        }

        private ZipArchiveEntry GetEntry(string fullEntryName)
        {
            return _zipArchive.Entries.SingleOrDefault(entry =>
                entry.FullName.Equals(fullEntryName, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}