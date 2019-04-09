using System.Net.Mime;

namespace KS.Fiks.ASiC_E.Model
{
    public static class AsiceConstants
    {
        public const string ContentTypeASiCe = "application/vnd.etsi.asic-e+zip";
        public const string ContentTypeSignature = "application/x-pkcs7-signature";
        public const string ContentTypeXml = MediaTypeNames.Text.Xml;
        public const string FileNameSignatureFile = "META-INF/signatures.xml";
        public const string FileNameMimeType = "mimetype";
        public const string SignatureAlgorithm = "SHA256WithRSA";
        public const string CadesManifestFilename = "META-INF/asicmanifest.xml";
        public static readonly MimeType MimeTypeCadesSignature = MimeType.ForString(ContentTypeSignature);
    }
}