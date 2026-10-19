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
        /// The value comes from the sender. The reader has checked that it is relative, uses '/' as separator and
        /// has no empty, '.' or '..' segments. It has not checked that the name is legal on the system you write
        /// to, and on Windows a name such as <c>NUL</c> is a device. Check the full path with
        /// <c>Path.GetFullPath</c> before you write the file to disk, see the README, and never pass the name to
        /// a shell.
        /// </summary>
        public string FullName => _zipArchiveEntry.FullName;

        public Stream OpenStream()
        {
            return new DigestReadStream(_zipArchiveEntry.Open(), FullName, _digestAlgorithm, _digestReceiver);
        }
    }
}