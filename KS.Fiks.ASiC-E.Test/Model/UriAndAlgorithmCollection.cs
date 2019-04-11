using System;
using KS.Fiks.ASiC_E.Model;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model
{
    public class UriAndAlgorithmCollection : TheoryData<Uri, MessageDigestAlgorithm>
    {
        public UriAndAlgorithmCollection()
        {
            Add(MessageDigestAlgorithm.UriSHA256XmlEnc, MessageDigestAlgorithm.SHA256);
            Add(MessageDigestAlgorithm.UriSHA384XmlEnc, MessageDigestAlgorithm.SHA384);
            Add(MessageDigestAlgorithm.UriSHA512XmlEnc, MessageDigestAlgorithm.SHA512);
            Add(MessageDigestAlgorithm.UriSHA256XmlDsig, MessageDigestAlgorithm.SHA256);
            Add(MessageDigestAlgorithm.UriSHA384XmlDsig, MessageDigestAlgorithm.SHA384);
            Add(MessageDigestAlgorithm.UriSHA512XmlDsig, MessageDigestAlgorithm.SHA512);
            Add(new Uri("http://localhost"), null);
            Add(null, null);
        }
    }
}