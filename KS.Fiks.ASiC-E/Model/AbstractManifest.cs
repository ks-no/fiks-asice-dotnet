using System;

namespace KS.Fiks.ASiC_E.Model
{
    public abstract class AbstractManifest
    {
        protected readonly ManifestSpec _manifestSpec;

        public AbstractManifest(ManifestSpec manifestSpec)
        {
            this._manifestSpec = manifestSpec;
        }
    }
}