using System;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class MimeTypeExtractorTest
    {
        [Theory(DisplayName = "Test MIME type extraction for known types")]
        [InlineData("filename.pdf", "application/pdf")]
        [InlineData("filename.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData("filename.doc", "application/msword")]
        [InlineData("filename.xls", "application/vnd.ms-excel")]
        [InlineData("filename.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData("filename.xml", "application/xml")]
        public void TestExtractMimeType(string fileName, string mimeType)
        {
            var type = MimeTypeExtractor.ExtractMimeType(fileName);

            type.ShouldNotBeNull().ShouldBeOfType<MimeType>();
            type.ToString().ShouldBe(mimeType);
        }

        [Fact(DisplayName = "Extract MIME type for PDF")]
        public void TestPdf()
        {
            var pdfType = MimeTypeExtractor.ExtractMimeType("filename.pdf");

            pdfType.ShouldNotBeNull().ShouldBeOfType<MimeType>();
            pdfType.ToString().ShouldBe("application/pdf");
        }

        [Theory(DisplayName = "Test MIME type extraction for unknown types (fallbacks to application/octet-stream)")]
        [InlineData("filename.xxs")]
        [InlineData("filename.xyy")]
        [InlineData("filename.illegal")]
        public void TestIllegalMimeType(string fileName)
        {
            var type = MimeTypeExtractor.ExtractMimeType(fileName);
            type.ShouldNotBeNull().ShouldBeOfType<MimeType>();

            type.ToString().ShouldBe(MimeMapping.MimeUtility.UnknownMimeType);
        }

        [Fact(DisplayName = "Test using null")]
        public void TestNull()
        {
            Action action = () => MimeTypeExtractor.ExtractMimeType(null);
            action.ShouldThrow<ArgumentNullException>();
        }
    }
}