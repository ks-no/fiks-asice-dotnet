using System;
using System.IO;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E
{
    public sealed class AsiceBuilder : IAsiceBuilder<AsiceArchive>
    {
        private readonly AsiceArchive asiceArchive;

        private AsiceBuilder(AsiceArchive asiceArchive, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            this.asiceArchive = asiceArchive;
            MessageDigestAlgorithm = messageDigestAlgorithm;
        }

        private MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public static AsiceBuilder Create(Stream stream, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            var outStream = stream ?? throw new ArgumentNullException(nameof(stream));
            var algorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));
            if (! outStream.CanWrite)
            {
                throw new ArgumentException("The provided Stream must be writable", nameof(stream));
            }

            return new AsiceBuilder(AsiceArchive.Create(outStream, new CadesManifestCreator()), algorithm);
        }

        public AsiceArchive Build()
        {

            return this.asiceArchive;
        }

        public IAsiceBuilder<AsiceArchive> AddFile(FileStream file)
        {
            return AddFile(file, file.Name);
        }

        public IAsiceBuilder<AsiceArchive> AddFile(Stream stream, string filename)
        {
            return AddFile(stream, filename, MimeTypeExtractor.ExtractMimeType(filename));
        }

        public IAsiceBuilder<AsiceArchive> AddFile(Stream stream, string filename, MimeType mimeType)
        {
            this.asiceArchive.AddEntry(stream, new FileRef(filename, mimeType));
            return this;
        }

        public IAsiceBuilder<AsiceArchive> Sign()
        {
            throw new NotImplementedException();
        }

        
        public void Dispose()
        {
            this.asiceArchive.Dispose();
        }
    }
}