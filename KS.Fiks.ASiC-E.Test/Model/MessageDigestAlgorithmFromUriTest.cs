using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class MessageDigestAlgorithmFromUriTest
    {
        [Theory]
        [ClassData(typeof(UriAndAlgorithmCollection))]
        public void TestFromUri(Uri uri, MessageDigestAlgorithm expectedAlgorithm)
        {
            MessageDigestAlgorithm.FromUri(uri).Should().Be(expectedAlgorithm);
        }
    }
}