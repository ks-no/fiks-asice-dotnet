using System;
using System.Collections.Generic;

namespace KS.Fiks.ASiC_E.Model
{
    public abstract class AbstractManifest
    {
        private readonly ManifestSpec _manifestSpec;

        public ManifestSpec Spec => this._manifestSpec;

        public abstract IEnumerable<SignatureFileRef> GetSignatureRefs();

        public abstract IDictionary<string, DeclaredDigestFile> GetDeclaredDigests();

        protected AbstractManifest(ManifestSpec manifestSpec)
        {
            this._manifestSpec = manifestSpec;
        }
    }
}