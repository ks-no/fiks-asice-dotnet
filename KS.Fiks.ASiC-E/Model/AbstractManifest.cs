using System;

namespace KS.Fiks.ASiC_E.Model
{
    public abstract class AbstractManifest
    {
        private readonly ManifestSpec _manifestSpec;

        public ManifestSpec Spec => this._manifestSpec;

        protected AbstractManifest(ManifestSpec manifestSpec)
        {
            this._manifestSpec = manifestSpec;
        }
    }
}