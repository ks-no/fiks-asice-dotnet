using System;
using System.IO;
using System.Text;
using FluentAssertions;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Manifest
{
    public class CadesManifestReaderTest
    {
        [Fact(DisplayName = "Parse valid CADES manifest")]
        public void ValidCadesManifest()
        {
            const string manifest = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ASiCManifest xmlns=""http://uri.etsi.org/2918/v1.2.1#"" xmlns:ns2=""http://www.w3.org/2000/09/xmldsig#"">
    <SigReference URI=""META-INF/signature-f741f40a-bfa5-477b-83ca-384ae074e002.p7s"" MimeType=""application/x-pkcs7-signature""/>
    <DataObjectReference URI=""bii-envelope.xml"" MimeType=""text/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>8A8ScSgd289FuAlzNX70A9LkpUu8wcseYTiSS1GESZc=</ns2:DigestValue>
    </DataObjectReference>
    <DataObjectReference URI=""bii-message.xml"" MimeType=""application/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>U+i/R1Sa/OqRASwozga2MCGDR8wd/7ZUGztmsm2kl50=</ns2:DigestValue>
    </DataObjectReference>
</ASiCManifest>";
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(manifest)))
            {
                var cadesManifestReader = new CadesManifestReader();
                var cadesManifest = cadesManifestReader.FromStream(inputStream);
                cadesManifest.Should().NotBeNull();
                cadesManifest.Digests.Count.Should().Be(2);
                cadesManifest.SignatureFileRef.Should().NotBeNull();
            }
        }

        [Fact(DisplayName = "Parse valid CADES with un-normalized namespace")]
        public void UnNormalizedManifest()
        {
            const string manifest = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ASiCManifest xmlns=""http://uri.etsi.org/02918/v1.2.1#"" xmlns:ns2=""http://www.w3.org/2000/09/xmldsig#"">
    <SigReference URI=""META-INF/signature-f741f40a-bfa5-477b-83ca-384ae074e002.p7s"" MimeType=""application/x-pkcs7-signature""/>
    <DataObjectReference URI=""bii-envelope.xml"" MimeType=""text/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>8A8ScSgd289FuAlzNX70A9LkpUu8wcseYTiSS1GESZc=</ns2:DigestValue>
    </DataObjectReference>
    <DataObjectReference URI=""bii-message.xml"" MimeType=""application/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>U+i/R1Sa/OqRASwozga2MCGDR8wd/7ZUGztmsm2kl50=</ns2:DigestValue>
    </DataObjectReference>
</ASiCManifest>";
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(manifest)))
            {
                var cadesManifestReader = new CadesManifestReader();
                var cadesManifest = cadesManifestReader.FromStream(inputStream);
                cadesManifest.Should().NotBeNull();
                cadesManifest.Digests.Count.Should().Be(2);
                cadesManifest.SignatureFileRef.Should().NotBeNull();
            }
        }

        [Fact(DisplayName = "Parse valid CADES with previous version of the schema")]
        public void ParseWithPreviousVersionSchema()
        {
            const string manifest = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ASiCManifest xmlns=""http://uri.etsi.org/02918/v1.1.1#"" xmlns:ns2=""http://www.w3.org/2000/09/xmldsig#"">
    <SigReference URI=""META-INF/signature-f741f40a-bfa5-477b-83ca-384ae074e002.p7s"" MimeType=""application/x-pkcs7-signature""/>
    <DataObjectReference URI=""bii-envelope.xml"" MimeType=""text/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>8A8ScSgd289FuAlzNX70A9LkpUu8wcseYTiSS1GESZc=</ns2:DigestValue>
    </DataObjectReference>
    <DataObjectReference URI=""bii-message.xml"" MimeType=""application/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>U+i/R1Sa/OqRASwozga2MCGDR8wd/7ZUGztmsm2kl50=</ns2:DigestValue>
    </DataObjectReference>
</ASiCManifest>";
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(manifest)))
            {
                var cadesManifestReader = new CadesManifestReader();
                var cadesManifest = cadesManifestReader.FromStream(inputStream);
                cadesManifest.Should().NotBeNull();
                cadesManifest.Digests.Count.Should().Be(2);
                cadesManifest.SignatureFileRef.Should().NotBeNull();
            }
        }

        [Fact(DisplayName = "Parse CADES with invalid namespace")]
        public void ParseWithInvalidNamespace()
        {
            const string manifest = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ASiCManifest xmlns=""http://uri.etsi.org/2918/v0.0.1#"" xmlns:ns2=""http://www.w3.org/2000/09/xmldsig#"">
    <SigReference URI=""META-INF/signature-f741f40a-bfa5-477b-83ca-384ae074e002.p7s"" MimeType=""application/x-pkcs7-signature""/>
    <DataObjectReference URI=""bii-envelope.xml"" MimeType=""text/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>8A8ScSgd289FuAlzNX70A9LkpUu8wcseYTiSS1GESZc=</ns2:DigestValue>
    </DataObjectReference>
    <DataObjectReference URI=""bii-message.xml"" MimeType=""application/xml"">
        <ns2:DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256""/>
        <ns2:DigestValue>U+i/R1Sa/OqRASwozga2MCGDR8wd/7ZUGztmsm2kl50=</ns2:DigestValue>
    </DataObjectReference>
</ASiCManifest>";
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(manifest)))
            {
                var cadesManifestReader = new CadesManifestReader();
                Func<CadesManifest> action = () => cadesManifestReader.FromStream(inputStream);
                action.Should().Throw<ManifestReadException>().And.Message.Should()
                    .Be(CadesManifestReader.ErrorMessageInvalidNamespace);
            }
        }
    }
}