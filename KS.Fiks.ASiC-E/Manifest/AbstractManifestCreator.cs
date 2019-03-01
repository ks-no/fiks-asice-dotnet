using System;
using System.Collections.Generic;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E.Manifest
{
    public abstract class AbstractManifestCreator : IManifestCreator
    {
        public abstract Model.ManifestContainer CreateManifest(IEnumerable<AsicPackageEntry> entries);

        protected static SignatureFileRef CreateSignatureRef()
        {
            var uuid = Guid.NewGuid().ToString();
            return new SignatureFileRef($"META-INF/signature-${uuid}.p7s", MimeType.ForString(AscieConstants.ContentTypeSignature));
        }
    }
}