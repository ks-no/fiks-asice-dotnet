using System;
using System.Collections.Generic;
using System.Linq;

namespace KS.Fiks.ASiC_E.Model
{
    public class DeclaredDigest
    {
        private readonly byte[] _digest;

        public MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public IEnumerable<byte> Digest => this._digest;

        public DeclaredDigest(byte[] digest, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            this._digest = digest ?? throw new ArgumentNullException(nameof(digest));
            MessageDigestAlgorithm = messageDigestAlgorithm ??
                                           throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        }

        public bool Verify(DeclaredDigest declaredDigest)
        {
            return MessageDigestAlgorithm == declaredDigest.MessageDigestAlgorithm &&
                   DigestEquals(declaredDigest.Digest);
        }

        private bool DigestEquals(IEnumerable<byte> digest)
        {
            return this._digest.SequenceEqual(digest);
        }
    }
}