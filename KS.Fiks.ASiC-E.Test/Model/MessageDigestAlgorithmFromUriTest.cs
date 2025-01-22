using System;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class MessageDigestAlgorithmFromUriTest
    {
        [Theory]
        [ClassData(typeof(UriAndAlgorithmCollection))]
        public void TestFromUri(Uri uri, MessageDigestAlgorithm expectedAlgorithm)
        {
            MessageDigestAlgorithm.FromUri(uri).ShouldBe(expectedAlgorithm);
        }
    }
}