using System;
using KS.Fiks.ASiC_E.Model;
using MimeMapping;

namespace KS.Fiks.ASiC_E;

public static class MimeTypeExtractor
{
    public static MimeType ExtractMimeType(string fileName)
    {
        var file = fileName ?? throw new ArgumentNullException(nameof(fileName));
        return MimeType.ForString(ExtractType(file));
    }

    private static string ExtractType(string fileName)
    {
        return MimeUtility.GetMimeMapping(fileName);
    }
}