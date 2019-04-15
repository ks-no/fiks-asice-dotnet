using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Sign;
using NLog;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Security;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsiceArchive : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ICertificateHolder SignatureCertificate { get; }

        private ZipArchive Archive { get;  }

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
            Log.Debug("Creating ASiC-e Zip");
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

            Log.Debug($"Adding entry '{entry.FileName}' of type '{entry.MimeType}' to the ASiC-e archive");

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
            using (var zipStream = zipEntry.Open())
            using (var digestStream = new DigestStream(zipStream, null, MessageDigestAlgorithm.Digest))
            {
                dataStream.CopyTo(digestStream);
                dataStream.Flush();
                entry.Digest = new DigestContainer(DigestUtilities.DoFinal(digestStream.WriteDigest()), MessageDigestAlgorithm);
            }

            return entry;
        }

        private void AddManifest()
        {
            Log.Debug("Creating manifest");
            var manifest = CreateManifest();
            if (manifest.ManifestSpec == ManifestSpec.Cades && manifest.SignatureFileRef != null)
            {
                var signatureFile = SignatureCreator.Create(SignatureCertificate).CreateCadesSignatureFile(manifest);
                manifest.SignatureFileRef = signatureFile.SignatureFileRef;
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

            Log.Debug("Manifest added to archive");
        }

        private ManifestContainer CreateManifest()
        {
            return this.manifestCreator.CreateManifest(this.entries);
        }
    }
}