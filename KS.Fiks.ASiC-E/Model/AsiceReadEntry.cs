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
        /// The value comes from the sender. The reader has checked that it is valid as defined by
        /// <see cref="AsiceEntryPath"/>: relative, with '/' as separator, without empty, '.' or '..' segments,
        /// without control characters and without reserved device names, so that <c>Path.Combine(directory, FullName)</c> stays inside
        /// <c>directory</c>. It has not checked that the name is legal on every operating system. Use
        /// <see cref="AsiceEntryPath.IsPortable"/> before you write the file to disk under this name, and never
        /// pass the name to a shell.
        /// </summary>
        public string FullName => _zipArchiveEntry.FullName;

        public Stream OpenStream()
        {
            return new DigestReadStream(_zipArchiveEntry.Open(), FullName, _digestAlgorithm, _digestReceiver);
        }
    }
}