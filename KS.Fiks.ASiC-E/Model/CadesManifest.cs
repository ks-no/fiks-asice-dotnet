namespace KS.Fiks.ASiC_E.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xsd;

    public class CadesManifest : AbstractManifest
    {
        private readonly ASiCManifestType _asiCManifestType;

        public string SignatureFileName => this._asiCManifestType?.SigReference?.URI;

        public SignatureFileRef SignatureFileRef =>
            this.SignatureFileName == null ? null : new SignatureFileRef(SignatureFileName);

        public IEnumerable<CalculatedDigest> Digests =>
            this._asiCManifestType?.DataObjectReference?.Select(d =>
                new CalculatedDigest(d.DigestValue, MessageDigestAlgorithm.FromUri(new Uri(d.DigestMethod.Algorithm))));

        public CadesManifest(ASiCManifestType asiCManifestType) : base(ManifestSpec.Cades)
        {
            this._asiCManifestType = asiCManifestType ?? throw new ArgumentNullException(nameof(asiCManifestType));
        }
    }
}