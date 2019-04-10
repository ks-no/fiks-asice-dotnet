using System;
using System.IO;
using KS.Fiks.ASiC_E.Model;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Security;

namespace KS.Fiks.ASiC_E.Crypto
{
    public class DigestReadStream : DigestStream
    {

        private readonly string _fileName;
        private readonly IDigestReceiver _digestReceiver;

        public DigestReadStream(Stream stream, string fileName, MessageDigestAlgorithm messageDigestAlgorithm, IDigestReceiver digestReceiver)
            : base(
                stream,
                messageDigestAlgorithm.Digest,
                null)
        {
            this._fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            this._digestReceiver = digestReceiver ?? throw new ArgumentNullException(nameof(digestReceiver));
        }

        public override void Close()
        {
            this._digestReceiver.ReceiveDigest(this._fileName, DigestUtilities.DoFinal(ReadDigest()));
            base.Close();
        }
    }
}