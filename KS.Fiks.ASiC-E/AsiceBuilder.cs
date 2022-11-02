using System;
using System.IO;
using System.Xml;
using KS.Fiks.ASiC_E.Crypto;
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

        /// <summary>
        /// Create builder
        /// </summary>
        /// <param name="stream">The stream where the ASiC-E data will be written. Can not be null and must be writable</param>
        /// <param name="messageDigestAlgorithm">The digest algorithm to use. Not nullable</param>
        /// <param name="signCertificate">A private/public keypair to use for signing. May be null</param>
        /// <returns>A builder that may be used to construct a ASiC-E package</returns>
        /// <exception cref="ArgumentException">If the provided stream is not writable</exception>
        public static AsiceBuilder Create(
            Stream stream,
            MessageDigestAlgorithm messageDigestAlgorithm,
            ICertificateHolder signCertificate)
        {
            var outStream = stream ?? throw new ArgumentNullException(nameof(stream));
            var algorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));
            if (! outStream.CanWrite)
            {
                throw new ArgumentException("The provided Stream must be writable", nameof(stream));
            }

            var cadesManifestCreator = signCertificate == null
                ? CadesManifestCreator.CreateWithoutSignatureFile()
                : CadesManifestCreator.CreateWithSignatureFile();
            return new AsiceBuilder(AsiceArchive.Create(outStream, cadesManifestCreator, signCertificate), algorithm);
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
            this.asiceArchive.AddEntry(stream, new FileRef(Path.GetFileName(filename), mimeType));
            return this;
        }
        
        public IAsiceBuilder<AsiceArchive> Finalize()
        {
            this.asiceArchive.Finalize();
            return this;
        }

        public void Dispose()
        {
            this.asiceArchive.Dispose();
        }
    }
}