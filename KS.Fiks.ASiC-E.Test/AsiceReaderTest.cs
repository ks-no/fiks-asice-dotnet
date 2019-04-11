using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace KS.Fiks.ASiC_E.Test
{
    public class AsiceReaderTest
    {
        [Fact(DisplayName = "Read from file")]
        public void ReadAndValidateAsice()
        {
            IAsicReader reader = new AsiceReader();
            using (var inputStream = TestDataUtil.ReadValidAsiceCadesFromResource())
            using (var asice = reader.Read(inputStream))
            {
                asice.Should().NotBeNull();
                foreach (var asiceReadEntry in asice.Entries)
                {
                    using (var entryStream = asiceReadEntry.OpenStream())
                    using (var bufferStream = new MemoryStream())
                    {
                        entryStream.CopyTo(bufferStream);
                        bufferStream.ToArray().Count().Should().BeGreaterThan(0);
                    }
                }

                asice.DigestVerifier.Verification().AllValid.Should().BeTrue();
            }
        }
    }
}