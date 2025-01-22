using System.IO;
using System.Linq;
using Shouldly;
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
                asice.ShouldNotBeNull();
                foreach (var asiceReadEntry in asice.Entries)
                {
                    using (var entryStream = asiceReadEntry.OpenStream())
                    using (var bufferStream = new MemoryStream())
                    {
                        entryStream.CopyTo(bufferStream);
                        bufferStream.ToArray().Count().ShouldBeGreaterThan(0);
                    }
                }

                asice.DigestVerifier.Verification().AllValid.ShouldBeTrue();
            }
        }
    }
}