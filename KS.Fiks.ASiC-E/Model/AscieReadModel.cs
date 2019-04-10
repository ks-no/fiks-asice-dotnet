using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;

namespace KS.Fiks.ASiC_E.Model
{
    public class AscieReadModel : IDisposable
    {
        private readonly ZipArchive _zipArchive;
        private readonly MessageDigestAlgorithm _digestAlgorithm;
        private readonly DigestVerifier _digestVerifier;

        private AscieReadModel(ZipArchive zipArchive, MessageDigestAlgorithm digestAlgorithm)
        {
            this._zipArchive = zipArchive;
            this._digestAlgorithm = digestAlgorithm;

            this.CadesManifest = GetCadesManifest();
            if (CadesManifest != null)
            {
                this._digestVerifier = Crypto.DigestVerifier.Create(CadesManifest.Digests);
            }

            this.Entries = GetAsiceEntries();
        }

        public IEnumerable<AsiceReadEntry> Entries { get; }

        public CadesManifest CadesManifest { get; }

        public IDigestVerifier DigestVerifier => this._digestVerifier;

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

        private IEnumerable<AsiceReadEntry> GetAsiceEntries()
        {
            return this._zipArchive.Entries.Where(entry => entry.Name != AsiceConstants.FileNameMimeType)
                .Where(entry => ! entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase))
                .Select(entry => new AsiceReadEntry(entry, this._digestAlgorithm, this._digestVerifier));
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