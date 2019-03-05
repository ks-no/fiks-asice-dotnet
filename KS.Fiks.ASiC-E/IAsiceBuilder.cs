using System;
using System.IO;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E
{
    public interface IAsiceBuilder<T> : IBuilder<T>, IDisposable
    {
        IAsiceBuilder<T> AddFile(FileStream file);

        IAsiceBuilder<T> AddFile(Stream stream, string filename);

        IAsiceBuilder<T> AddFile(Stream stream, string filename, MimeType mimeType);

        IAsiceBuilder<T> AddSignatureCertificate(ICertificateHolder certificateHolder);
    }
}