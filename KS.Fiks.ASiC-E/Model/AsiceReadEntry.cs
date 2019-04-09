using System;
using System.IO;
using System.IO.Compression;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.IO;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsiceReadEntry
    {
        private readonly ZipArchiveEntry _zipArchiveEntry;
        private readonly MessageDigestAlgorithm _digestAlgorithm;
        private IDigest _digest;

        public AsiceReadEntry(ZipArchiveEntry zipArchiveEntry, MessageDigestAlgorithm digestAlgorithm)
        {
            _zipArchiveEntry = zipArchiveEntry ?? throw new ArgumentNullException(nameof(zipArchiveEntry));
            _digestAlgorithm = digestAlgorithm ?? throw new ArgumentNullException(nameof(digestAlgorithm));
        }

        public string FileName => _zipArchiveEntry.Name;

        public string Digest => _digest?.ToString();

        public Stream OpenStream()
        {
            this._digest = this._digestAlgorithm.Digest;
            return new DigestStream(this._zipArchiveEntry.Open(), this._digest, null);
        }
    }
}