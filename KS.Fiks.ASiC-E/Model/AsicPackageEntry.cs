using System;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsicPackageEntry
    {
        public string FileName { get; }

        public MimeType Type { get; }

        public MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public byte[] Digest { get; set; }

        public AsicPackageEntry(string fileName, MimeType type, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            MessageDigestAlgorithm = messageDigestAlgorithm ??
                                     throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        }
    }
}