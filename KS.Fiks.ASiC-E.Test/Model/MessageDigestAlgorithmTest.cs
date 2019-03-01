using System.Collections.Generic;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class MessageDigestAlgorithmTest
    {
        public static IEnumerable<object[]> algorithms = new[]
        {
            new object[] {MessageDigestAlgorithm.SHA256}, 
            new object[] {MessageDigestAlgorithm.SHA384},
            new object[] {MessageDigestAlgorithm.SHA512}
        };

        [Theory]
        [MemberData(nameof(algorithms))]
        public void TestStaticProperties(MessageDigestAlgorithm messageDigestAlgorithm)
        {
            messageDigestAlgorithm.Name.Should().NotBeNull();
            messageDigestAlgorithm.Uri.Should().NotBeNull();
            messageDigestAlgorithm.Digest.Should().NotBeNull();
        }
    }
}