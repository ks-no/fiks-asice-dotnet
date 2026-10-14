using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Manifest;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Sign;
using KS.Fiks.ASiC_E.Xsd;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Sign;

public class XadesSignatureCreatorTest
{
    // The issuer of the test certificate has the non-ISO country code
    // "C=Unknown", which BouncyCastle 2.7.0 rejects when an X509Name is
    // rebuilt from strings. The expected value is what the implementation
    // before BouncyCastle 2.7.0 produced, so the format must stay unchanged.
    private const string ExpectedIssuerName =
        "2.5.4.6=Unknown,2.5.4.8=Unknown,2.5.4.7=Unknown,2.5.4.10=Unknown,2.5.4.11=Unknown,2.5.4.3=Unknown";

    [Fact(DisplayName = "XAdES signature contains the issuer name of the signing certificate")]
    public void SignatureContainsIssuerName()
    {
        var certHolder = TestdataLoader.ReadCertificatesForTest();

        using var zipStream = new MemoryStream();
        using (var archive = new AsiceArchive(
            zipStream,
            new CadesManifestCreator(),
            new XadesSignature(),
            XadesSignatureCreator.Create(certHolder, null)))
        {
            archive.AddEntry(
                new MemoryStream(Encoding.UTF8.GetBytes("Hello")),
                new FileRef("hello.txt", MimeType.ForString("text/plain")));
        }

        using var zipInput = new MemoryStream(zipStream.ToArray());
        using var zipArchive = new ZipArchive(zipInput, ZipArchiveMode.Read);
        using var signatureStream = zipArchive.GetEntry("META-INF/signatures.xml").Open();

        var issuerName = XDocument.Load(signatureStream)
            .Descendants()
            .Single(e => e.Name.LocalName == "X509IssuerName")
            .Value;

        issuerName.ShouldBe(ExpectedIssuerName);
    }

    [Fact(DisplayName = "XAdES signature can be read with XAdESSignaturesType")]
    public void SignatureMatchesSchemaType()
    {
        var certHolder = TestdataLoader.ReadCertificatesForTest();

        using var zipStream = new MemoryStream();
        using (var archive = new AsiceArchive(
            zipStream,
            new CadesManifestCreator(),
            new XadesSignature(),
            XadesSignatureCreator.Create(certHolder, null)))
        {
            archive.AddEntry(
                new MemoryStream(Encoding.UTF8.GetBytes("Hello")),
                new FileRef("hello.txt", MimeType.ForString("text/plain")));
        }

        using var zipInput = new MemoryStream(zipStream.ToArray());
        using var zipArchive = new ZipArchive(zipInput, ZipArchiveMode.Read);
        using var signatureStream = zipArchive.GetEntry("META-INF/signatures.xml").Open();

        var signatures = (XAdESSignaturesType)new XmlSerializer(typeof(XAdESSignaturesType)).Deserialize(signatureStream);
        signatures.Signature.ShouldHaveSingleItem();
    }
}
