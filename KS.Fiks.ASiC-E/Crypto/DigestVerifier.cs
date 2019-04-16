using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KS.Fiks.ASiC_E.Model;
using NLog;

namespace KS.Fiks.ASiC_E.Crypto
{
    public class DigestVerifier : IDigestReceiver, IDigestVerifier
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, DeclaredDigestFile> _declaredDigests;
        private readonly Queue<string> _validFiles = new Queue<string>();
        private readonly Queue<string> _invalidFiles = new Queue<string>();

        private DigestVerifier(IDictionary<string, DeclaredDigestFile> declaredDigests)
        {
            this._declaredDigests = declaredDigests ?? throw new ArgumentNullException(nameof(declaredDigests));
        }

        public static DigestVerifier Create(IDictionary<string, DeclaredDigestFile> declaredDigests)
        {
            var dd = declaredDigests ?? throw new ArgumentNullException(nameof(declaredDigests));
            return new DigestVerifier(dd);
        }

        public void ReceiveDigest(string fileName, byte[] digest)
        {
            _log.Debug(CultureInfo.CurrentCulture, "Got digest for file {fileName}", fileName);
            var declaredDigest = _declaredDigests[fileName];
            if (declaredDigest != null)
            {
                if (declaredDigest.Digest.SequenceEqual(digest))
                {
                    _log.Debug(CultureInfo.CurrentCulture, "Digest verified for file {fileName}", fileName);
                    this._validFiles.Enqueue(fileName);
                }
                else
                {
                    _log.Debug(CultureInfo.CurrentCulture, "Digest did not match the declared digest for file {fileName}", fileName);
                    this._invalidFiles.Enqueue(fileName);
                }
            }
            else
            {
                _log.Debug(CultureInfo.CurrentCulture, "No file named {fileName} has been declared. It will be deemed invalid", fileName);
                this._invalidFiles.Enqueue(fileName);
            }
        }

        public DigestVerificationResult Verification()
        {
            var totalFilesProcessedCount = this._invalidFiles.Count + this._validFiles.Count;
            if (totalFilesProcessedCount != this._declaredDigests.Count)
            {
                throw new DigestVerificationException($"Total number of files processed by the verifier was {totalFilesProcessedCount}, but {this._declaredDigests.Count} files was declared");
            }

            return new DigestVerificationResult(this._validFiles, this._invalidFiles);
        }
    }
}