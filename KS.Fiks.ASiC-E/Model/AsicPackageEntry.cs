using System;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsicPackageEntry
    {
        public string FileName { get; }

        public MimeType Type { get; }

        public MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        private byte[] _digest;

        public byte[] Digest
        {
            get => this._digest;
            set => this._digest = value;
        }

        public AsicPackageEntry(string fileName, MimeType type, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            MessageDigestAlgorithm = messageDigestAlgorithm ??
                                     throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        }
    }
}