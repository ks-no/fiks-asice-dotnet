using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Sign;
using KS.Fiks.ASiC_E.Xsd;
using Org.BouncyCastle.Crypto.IO;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsiceArchive : IDisposable
    {
        private ZipArchive Archive { get;  }

        private ICertificateHolder SignatureCertificate { get; }

        private readonly Queue<AsicePackageEntry> entries = new Queue<AsicePackageEntry>();

        private readonly IManifestCreator manifestCreator;

        private MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public AsiceArchive(ZipArchive archive, IManifestCreator creator, MessageDigestAlgorithm messageDigestAlgorithm, ICertificateHolder signatureCertificate)
        {
            Archive = archive ?? throw new ArgumentNullException(nameof(archive));
            this.manifestCreator = creator ?? throw new ArgumentNullException(nameof(creator));
            MessageDigestAlgorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));
            SignatureCertificate = signatureCertificate;
        }

        public static AsiceArchive Create(Stream zipOutStream, IManifestCreator manifestCreator, ICertificateHolder signatureCertificateHolder)
        {
            var zipArchive = new ZipArchive(zipOutStream, ZipArchiveMode.Create, false, Encoding.UTF8);

            // Add mimetype entry
            var zipArchiveEntry = zipArchive.CreateEntry(AsiceConstants.FileNameMimeType);

            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(Encoding.UTF8.GetBytes(AsiceConstants.ContentTypeASiCe));
            }

            return new AsiceArchive(zipArchive, manifestCreator, MessageDigestAlgorithm.SHA256, signatureCertificateHolder);
        }

        /// <summary>
        /// Add file to ASiC-E package
        /// </summary>
        /// <param name="contentStream">The stream that contains the data</param>
        /// <param name="entry">A description of the file entry</param>
        /// <returns>The archive with the entry added</returns>
        /// <exception cref="ArgumentException">If any if the parameters is null or invalid.
        /// Only files that are not in the /META-INF may be added</exception>
        public AsiceArchive AddEntry(Stream contentStream, FileRef entry)
        {
            var packageEntry = entry ?? throw new ArgumentNullException(nameof(entry), "Entry must be provided");
            if (packageEntry.FileName.StartsWith("META-INF/", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException("Adding files to META-INF is not allowed.");
            }

            this.entries.Enqueue(CreateEntry(contentStream, new AsicePackageEntry(entry.FileName, entry.MimeType, MessageDigestAlgorithm)));
            return this;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            AddManifest();

            Archive.Dispose();
        }

        private AsicePackageEntry CreateEntry(Stream contentStream, AsicePackageEntry entry)
        {
            var fileName = entry.FileName ?? throw new ArgumentNullException(nameof(entry), "File name must be provided");
            var dataStream = contentStream ?? throw new ArgumentNullException(nameof(contentStream));
            var zipEntry = Archive.CreateEntry(fileName);
            using (var digestStream = new DigestStream(dataStream, MessageDigestAlgorithm.Digest, MessageDigestAlgorithm.Digest))
            using (var zipStream = zipEntry.Open())
            {
                var digest = new byte[digestStream.WriteDigest().GetDigestSize()];
                digestStream.CopyTo(zipStream);
                digestStream.WriteDigest().DoFinal(digest, 0);
                entry.Digest = new DigestContainer(digest, MessageDigestAlgorithm);
            }

            return entry;
        }

        private void AddManifest()
        {
            var manifest = CreateManifest();
            if (manifest.ManifestSpec == ManifestSpec.Cades)
            {
                var signatureFile = SignatureCreator.Create(SignatureCertificate).CreateCadesSignatureFile(manifest);
                using (var signatureStream = new MemoryStream(signatureFile.Data))
                {
                    var entry = Archive.CreateEntry(signatureFile.SignatureFileRef.FileName);
                    using (var zipEntryStream = entry.Open())
                    {
                        signatureStream.CopyTo(zipEntryStream);
                    }
                }
            }

            using (var manifestStream = new MemoryStream(manifest.Data))
            {
                CreateEntry(manifestStream, new AsicePackageEntry(manifest.FileName, MimeType.ForString(AsiceConstants.ContentTypeXml), MessageDigestAlgorithm));
            }
        }

        private ManifestContainer CreateManifest()
        {
            return this.manifestCreator.CreateManifest(this.entries);
        }
    }
}