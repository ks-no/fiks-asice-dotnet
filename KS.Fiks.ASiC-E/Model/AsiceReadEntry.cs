using System;
using System.IO;
using System.IO.Compression;
using KS.Fiks.ASiC_E.Crypto;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.IO;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsiceReadEntry
    {
        private readonly ZipArchiveEntry _zipArchiveEntry;
        private readonly MessageDigestAlgorithm _digestAlgorithm;
        private readonly IDigestReceiver _digestReceiver;

        public AsiceReadEntry(ZipArchiveEntry zipArchiveEntry, MessageDigestAlgorithm digestAlgorithm, IDigestReceiver digestReceiver)
        {
            this._zipArchiveEntry = zipArchiveEntry ?? throw new ArgumentNullException(nameof(zipArchiveEntry));
            this._digestAlgorithm = digestAlgorithm ?? throw new ArgumentNullException(nameof(digestAlgorithm));
            this._digestReceiver = digestReceiver;
        }

        public string FileName => _zipArchiveEntry.Name;

        public Stream OpenStream()
        {
            return new DigestReadStream(this._zipArchiveEntry.Open(), FileName, this._digestAlgorithm, this._digestReceiver);
        }
    }
}