using System;
using System.Collections.Generic;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Manifest
{
    public abstract class AbstractManifestCreator : IManifestCreator
    {
        public abstract ManifestContainer CreateManifest(
            IEnumerable<AsicePackageEntry> entries,
            SignatureFileRef signatureFileRef);

        protected static SignatureFileRef CreateSignatureRef()
        {
            var uuid = Guid.NewGuid().ToString();
            return new SignatureFileRef($"META-INF/signature-{uuid}.p7s");
        }
    }
}