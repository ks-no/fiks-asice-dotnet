using System;
using MimeMapping;

namespace KS.Fiks.ASiC_E
{
    using Model;

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
}