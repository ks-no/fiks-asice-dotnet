using System;
using System.IO;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
using Moq;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class AsiceBuilderTest
    {
        [Fact(DisplayName = "Try to create builder using non-writable stream")]
        public void TestNotWritableStream()
        {
            var zipStream = new Mock<Stream>();
            Action createFunction = () => AsiceBuilder.Create(zipStream.Object, MessageDigestAlgorithm.SHA512);
            createFunction.Should().Throw<ArgumentException>();
            zipStream.VerifyGet(s => s.CanWrite);
            zipStream.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestAddFileStream()
        {
            using (var zipStream = new MemoryStream())
            using (var fileStream = new FileStream("small.pdf", FileMode.Open))
            {
                var asiceBuilder = AsiceBuilder.Create(zipStream, MessageDigestAlgorithm.SHA512);
                asiceBuilder.Should().NotBeNull();

                asiceBuilder.AddFile(fileStream).Should().NotBeNull().And.BeOfType<AsiceBuilder>();

                var asiceArchive = asiceBuilder.Build();
                asiceArchive.Should().NotBeNull();
                var zippedBytes = zipStream.ToArray();
                zippedBytes.Should().HaveCountGreaterThan(0);
            }
        }
    }
}