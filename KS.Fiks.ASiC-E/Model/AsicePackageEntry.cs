using System;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsicePackageEntry
    {
        public string FileName { get; }

        public MimeType Type { get; }

        public MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public bool RootFile { get; }

        public DigestContainer Digest { get; set; }

        public AsicePackageEntry(string fileName, MimeType type, MessageDigestAlgorithm messageDigestAlgorithm, bool rootFile = false)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            MessageDigestAlgorithm = messageDigestAlgorithm ??
                                     throw new ArgumentNullException(nameof(messageDigestAlgorithm));
            RootFile = rootFile;
        }
    }
}