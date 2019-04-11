using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
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
            messageDigestAlgorithm.Name.Should().NotBeNull();
            messageDigestAlgorithm.Uri.Should().NotBeNull();
            messageDigestAlgorithm.Digest.Should().NotBeNull();
        }

        [Fact(DisplayName = "Test that the static field UriSHA256 is initialized")]
        public void Uri256()
        {
            MessageDigestAlgorithm.UriSHA256XmlEnc.Should().NotBeNull();
        }

        [Fact(DisplayName = "Test that the static field UriSHA384 is initialized")]
        public void Uri384()
        {
            MessageDigestAlgorithm.UriSHA384XmlEnc.Should().NotBeNull();
        }

        [Fact(DisplayName = "Test that the static field UriSHA512 is initialized")]
        public void Uri512()
        {
            MessageDigestAlgorithm.UriSHA512XmlEnc.Should().NotBeNull();
        }
    }
}