using System;

namespace KS.Fiks.ASiC_E
{
    using KS.Fiks.ASiC_E.Model;

    public static class MimeTypeExtractor
    {
        public static MimeType ExtractMimeType(string fileName)
        {
            var file = fileName ?? throw new ArgumentNullException(nameof(fileName));
            return MimeType.ForString(ExtractType(file));
        }

        private static string ExtractType(string fileName)
        {
            return MimeTypes.GetMimeType(fileName);
        }
    }
}