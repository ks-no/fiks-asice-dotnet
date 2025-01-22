using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class MessageDigestAlgorithmTest
    {
        [SuppressMessage("Microsoft.Usage", "CA2211", Justification = "Needs to be public to support XUnit")]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Needs to be public to support XUnit")]
        public static IEnumerable<object[]> Algorithms = new[]
        {
            new object[] { MessageDigestAlgorithm.SHA256 },
            new object[] { MessageDigestAlgorithm.SHA384 },
            new object[] { MessageDigestAlgorithm.SHA512 }
        };

        [Theory]
        [MemberData(nameof(Algorithms))]
        public void TestStaticProperties(MessageDigestAlgorithm messageDigestAlgorithm)
        {
            messageDigestAlgorithm.Name.ShouldNotBeNull();
            messageDigestAlgorithm.Uri.ShouldNotBeNull();
            messageDigestAlgorithm.Digest.ShouldNotBeNull();
        }

        [Fact(DisplayName = "Test that the static field UriSHA256 is initialized")]
        public void Uri256()
        {
            MessageDigestAlgorithm.UriSHA256XmlEnc.ShouldNotBeNull();
        }

        [Fact(DisplayName = "Test that the static field UriSHA384 is initialized")]
        public void Uri384()
        {
            MessageDigestAlgorithm.UriSHA384XmlEnc.ShouldNotBeNull();
        }

        [Fact(DisplayName = "Test that the static field UriSHA512 is initialized")]
        public void Uri512()
        {
            MessageDigestAlgorithm.UriSHA512XmlEnc.ShouldNotBeNull();
        }
    }
}