using System;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsicePackageEntry
    {
        private static int identCounter = 0;

        public string FileName { get; }

        public string ID
        {
            get
            {
                return $"ID_{id}";
            }
        }

        private readonly int id;

        public MimeType Type { get; }

        public MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public DigestContainer Digest { get; set; }

        public AsicePackageEntry(string fileName, MimeType type, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            MessageDigestAlgorithm = messageDigestAlgorithm ??
                                     throw new ArgumentNullException(nameof(messageDigestAlgorithm));
            id = identCounter++;
        }
    }
}