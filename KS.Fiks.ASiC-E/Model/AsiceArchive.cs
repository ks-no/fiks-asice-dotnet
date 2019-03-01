using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Xsd;
using Org.BouncyCastle.Crypto.IO;

namespace KS.Fiks.ASiC_E.Model
{
    public class AsiceArchive : IDisposable
    {
        private ZipArchive Archive { get;  }

        private readonly Queue<AsicPackageEntry> _entries = new Queue<AsicPackageEntry>();

        private readonly IManifestCreator _manifestCreator;

        private MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public AsiceArchive(ZipArchive archive, IManifestCreator creator, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            Archive = archive ?? throw new ArgumentNullException(nameof(archive));
            this._manifestCreator = creator ?? throw new ArgumentNullException(nameof(creator));
            MessageDigestAlgorithm = messageDigestAlgorithm ?? throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        }

        public static AsiceArchive Create(Stream zipOutStream, IManifestCreator manifestCreator)
        {
            var zipArchive = new ZipArchive(zipOutStream, ZipArchiveMode.Create, false, Encoding.UTF8);
            var zipArchiveEntry = zipArchive.CreateEntry("mimetype");

            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(Encoding.UTF8.GetBytes(AscieConstants.ContentTypeASiCe));
            }

            return new AsiceArchive(zipArchive, manifestCreator, MessageDigestAlgorithm.SHA256);
        }

        /**
         * Add file to ASiC-E package
         */
        public AsiceArchive AddEntry(Stream contentStream, FileRef entry)
        {
            var packageEntry = entry ?? throw new ArgumentNullException(nameof(entry), "Entry must be provided");
            if (packageEntry.FileName.StartsWith("META-INF/", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException("Adding files to META-INF is not allowed.");
            }

            this._entries.Enqueue(CreateEntry(contentStream, new AsicPackageEntry(entry.FileName, entry.MimeType, MessageDigestAlgorithm)));
            return this;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            Archive.Dispose();
        }

        private AsicPackageEntry CreateEntry(Stream contentStream, AsicPackageEntry entry)
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
                entry.Digest = digest;
            }

            return entry;
        }

        private void AddManifest()
        {
            var manifest = CreateManifest();
            using (var manifestStream = new MemoryStream(manifest.Data))
            {
                CreateEntry(manifestStream, new AsicPackageEntry(manifest.FileName, MimeType.ForString(AscieConstants.ContentTypeXml), MessageDigestAlgorithm));
            }
        }

        private ManifestContainer CreateManifest()
        {
            return this._manifestCreator.CreateManifest(this._entries);
        }
    }
}