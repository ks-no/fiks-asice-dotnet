using System;
using System.IO;
using System.IO.Compression;
using KS.Fiks.ASiC_E.Crypto;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsiceReadEntry
    {
        private readonly ZipArchiveEntry _zipArchiveEntry;
        private readonly MessageDigestAlgorithm _digestAlgorithm;
        private readonly IDigestReceiver _digestReceiver;

        public AsiceReadEntry(ZipArchiveEntry zipArchiveEntry, MessageDigestAlgorithm digestAlgorithm, IDigestReceiver digestReceiver)
        {
            _zipArchiveEntry = zipArchiveEntry ?? throw new ArgumentNullException(nameof(zipArchiveEntry));
            _digestAlgorithm = digestAlgorithm ?? throw new ArgumentNullException(nameof(digestAlgorithm));
            _digestReceiver = digestReceiver;
        }

        /// <summary>
        /// The name of the file, without the folders it is in
        /// </summary>
        public string FileName => _zipArchiveEntry.Name;

        /// <summary>
        /// The path of the file in the package, including folders, for example <c>root/someFolder/small.pdf</c>.
        /// The value comes from the sender. Check it before you use it as a path on disk.
        /// </summary>
        public string FullName => _zipArchiveEntry.FullName;

        public Stream OpenStream()
        {
            return new DigestReadStream(_zipArchiveEntry.Open(), FullName, _digestAlgorithm, _digestReceiver);
        }
    }
}