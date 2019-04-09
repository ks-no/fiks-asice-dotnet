using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E.Model
{
    public class CadesManifest : AbstractManifest
    {
        public SignatureFileContainer SignatureFile { get; set; }

        private readonly ASiCManifestType _asiCManifestType;

        public string SignatureFileName => this._asiCManifestType?.SigReference?.URI;

        public IEnumerable<CalculatedDigest> Digests =>
            this._asiCManifestType?.DataObjectReference.Select(d =>
                new CalculatedDigest(d.DigestValue, MessageDigestAlgorithm.FromUri(new Uri(d.DigestMethod.Algorithm))));

        public CadesManifest(ASiCManifestType asiCManifestType) : base(ManifestSpec.Cades)
        {
            this._asiCManifestType = asiCManifestType ?? throw new ArgumentNullException(nameof(asiCManifestType));
        }
    }
}