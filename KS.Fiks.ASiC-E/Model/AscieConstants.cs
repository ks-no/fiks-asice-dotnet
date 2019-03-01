using System.Net.Mime;

namespace KS.Fiks.ASiC_E.Model
{
    internal static class AscieConstants
    {
        public const string ContentTypeASiCe = "application/vnd.etsi.asic-e+zip";
        public const string ContentTypeSignature = "application/x-pkcs7-signature";
        public const string ContentTypeXml = MediaTypeNames.Text.Xml;
    }
}