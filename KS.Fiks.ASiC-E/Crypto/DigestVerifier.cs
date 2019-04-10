using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common.Logging;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Crypto
{
    public class DigestVerifier : IDigestReceiver, IDigestVerifier
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DigestVerifier));
        private readonly IDictionary<string, DeclaredDigest> _declaredDigests;
        private readonly Queue<string> _validFiles = new Queue<string>();
        private readonly Queue<string> _invalidFiles = new Queue<string>();

        private DigestVerifier(IDictionary<string, DeclaredDigest> declaredDigests)
        {
            this._declaredDigests = declaredDigests ?? throw new ArgumentNullException(nameof(declaredDigests));
        }

        public static DigestVerifier Create(IDictionary<string, DeclaredDigest> declaredDigests)
        {
            var dd = declaredDigests ?? throw new ArgumentNullException(nameof(declaredDigests));
            return new DigestVerifier(dd);
        }

        public void ReceiveDigest(string fileName, byte[] digest)
        {
            _log.Debug(CultureInfo.CurrentCulture, m => m("Got digest for file '{0}'", fileName));
            var declaredDigest = _declaredDigests[fileName];
            if (declaredDigest != null)
            {
                if (declaredDigest.Digest.SequenceEqual(digest))
                {
                    _log.Debug(CultureInfo.CurrentCulture, m => m("Digest verified for file '{0}'", fileName));
                    this._validFiles.Enqueue(fileName);
                }
                else
                {
                    _log.Debug(CultureInfo.CurrentCulture, m => m("Digest did not match the declared digest for file '{0}'", fileName));
                    this._invalidFiles.Enqueue(fileName);
                }
            }
            else
            {
                _log.Debug(CultureInfo.CurrentCulture, m => m("No file named '{0}' has been declared. It will be deemed invalid"));
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