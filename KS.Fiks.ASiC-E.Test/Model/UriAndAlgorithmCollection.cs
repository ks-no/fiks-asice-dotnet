using System;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class UriAndAlgorithmCollection : TheoryData<Uri, MessageDigestAlgorithm>
    {
        public UriAndAlgorithmCollection()
        {
            Add(MessageDigestAlgorithm.UriSHA256, MessageDigestAlgorithm.SHA256);
            Add(MessageDigestAlgorithm.UriSHA384, MessageDigestAlgorithm.SHA384);
            Add(MessageDigestAlgorithm.UriSHA512, MessageDigestAlgorithm.SHA512);
            Add(new Uri("http://localhost"), null);
            Add(null, null);
        }
    }
}