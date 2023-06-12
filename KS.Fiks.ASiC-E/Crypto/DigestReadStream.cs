using System;
using System.IO;
using KS.Fiks.ASiC_E.Model;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Security;

namespace KS.Fiks.ASiC_E.Crypto
{
    public sealed class DigestReadStream : Stream
    {

        private readonly string _fileName;
        private readonly IDigestReceiver _digestReceiver;

        private readonly DigestStream _digestStream;

        public DigestReadStream(Stream stream, string fileName, MessageDigestAlgorithm messageDigestAlgorithm, IDigestReceiver digestReceiver)
        {
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _digestReceiver = digestReceiver ?? throw new ArgumentNullException(nameof(digestReceiver));

            _digestStream = new DigestStream(stream, messageDigestAlgorithm.Digest, null);
        }

        public override void Close()
        {
            _digestReceiver.ReceiveDigest(_fileName, DigestUtilities.DoFinal(_digestStream.ReadDigest));
            _digestStream.Close();
        }

        public new void Dispose()
        {
            _digestStream.Dispose();
        }

        public override void Flush()
        {
            _digestStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _digestStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _digestStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _digestStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _digestStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _digestStream.CanRead;

        public override bool CanSeek => _digestStream.CanSeek;

        public override bool CanWrite => _digestStream.CanWrite;

        public override long Length => _digestStream.Length;

        public override long Position
        {
            get => _digestStream.Position;
            set => _digestStream.Position = value;
        }
    }
}