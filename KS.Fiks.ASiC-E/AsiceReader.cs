using System.IO;
using System.IO.Compression;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E
{
    public class AsiceReader : IAsicReader
    {
        public AsiceReadModel Read(Stream inputStream)
        {
            var zipArchive = new ZipArchive(inputStream, ZipArchiveMode.Read);
            return AsiceReadModel.Create(zipArchive);
        }
    }
}