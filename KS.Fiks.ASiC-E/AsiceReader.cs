using System.IO;
using System.IO.Compression;
using KS.Fiks.ASiC_E.Model;
using Microsoft.Extensions.Logging;

namespace KS.Fiks.ASiC_E;

public class AsiceReader : IAsicReader
{
    private readonly ILoggerFactory _loggerFactory;

    public AsiceReader(ILoggerFactory loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    public AsiceReadModel Read(Stream inputStream)
    {
        var zipArchive = new ZipArchive(inputStream, ZipArchiveMode.Read);
        return new AsiceReadModel(zipArchive, _loggerFactory);
    }
}