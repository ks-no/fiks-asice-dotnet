using System;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
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
        public void testExtractMimeType(string fileName, string mimeType)
        {
            var type = MimeTypeExtractor.ExtractMimeType(fileName);

            type.Should()
                .NotBeNull()
                .And
                .BeOfType<MimeType>();
            type.ToString().Should().Be(mimeType);
        }

        [Fact(DisplayName = "Extract MIME type for PDF")]
        public void testPDF()
        {
            var pdfType = MimeTypeExtractor.ExtractMimeType("filename.pdf");

            pdfType.Should()
                .NotBeNull()
                .And
                .BeOfType<MimeType>();

            pdfType.ToString().Should().Be("application/pdf");
        }

        [Theory(DisplayName = "Test MIME type extraction for unknown types (fallbacks to application/octet-stream)")]
        [InlineData("filename.xxs")]
        [InlineData("filename.xyy")]
        [InlineData("filename.illegal")]
        public void testIllegalMimeType(string fileName)
        {
            var type = MimeTypeExtractor.ExtractMimeType(fileName);
            type.Should()
                .NotBeNull()
                .And
                .BeOfType<MimeType>();

            type.ToString().Should().Be(MimeTypes.FallbackMimeType);
        }

        [Fact(DisplayName = "Test using null")]
        public void testNull()
        {
            Action action = () => MimeTypeExtractor.ExtractMimeType(null);
            action.Should().Throw<ArgumentNullException>();
        }
    }

}