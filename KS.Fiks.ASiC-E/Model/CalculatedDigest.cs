using System;
using System.Collections.Generic;
using System.Linq;

namespace KS.Fiks.ASiC_E.Model
{
    public class CalculatedDigest
    {
        private readonly byte[] _digest;

        public MessageDigestAlgorithm MessageDigestAlgorithm { get; }

        public IEnumerable<byte> Digest => this._digest;

        public CalculatedDigest(byte[] digest, MessageDigestAlgorithm messageDigestAlgorithm)
        {
            this._digest = digest ?? throw new ArgumentNullException(nameof(digest));
            MessageDigestAlgorithm = messageDigestAlgorithm ??
                                           throw new ArgumentNullException(nameof(messageDigestAlgorithm));
        }

        public bool Verify(CalculatedDigest calculatedDigest)
        {
            return MessageDigestAlgorithm == calculatedDigest.MessageDigestAlgorithm &&
                   DigestEquals(calculatedDigest.Digest);
        }

        private bool DigestEquals(IEnumerable<byte> digest)
        {
            return this._digest.SequenceEqual(digest);
        }
    }
}