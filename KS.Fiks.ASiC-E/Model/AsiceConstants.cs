using System.Net.Mime;

namespace KS.Fiks.ASiC_E.Model
{
    public static class AsiceConstants
    {
        public const string ContentTypeASiCe = "application/vnd.etsi.asic-e+zip";
        public const string ContentTypeSignature = "application/x-pkcs7-signature";

        // TODO: Switch to using MediaTypeNames.Application.Xml instead of
        // string literal if netstandard2.0 is dropped from TargetFrameworks:
        public const string ContentTypeApplicationXml = "application/xml";
        public const string ContentTypeXml = MediaTypeNames.Text.Xml;
        public const string FileNameSignatureFile = "META-INF/signatures.xml";
        public const string FileNameMimeType = "mimetype";
        public const string SignatureAlgorithm = "SHA256WithRSA";
        public const string CadesManifestFilename = "META-INF/asicmanifest.xml";
        public static readonly MimeType MimeTypeCadesSignature = MimeType.ForString(ContentTypeSignature);
        public static readonly MimeType MimeTypeXadesSignature = MimeType.ForString(ContentTypeApplicationXml);
    }
}