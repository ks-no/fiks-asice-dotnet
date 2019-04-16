using System.Collections.Immutable;

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

        public IDictionary<string, DeclaredDigestFile> Digests { get; }

        public string RootFile { get; }

        public override IEnumerable<SignatureFileRef> GetSignatureRefs()
        {
            return SignatureFileRef == null
                ? Enumerable.Empty<SignatureFileRef>()
                : ImmutableList.Create(SignatureFileRef);
        }

        public override IDictionary<string, DeclaredDigestFile> GetDeclaredDigests()
        {
            return Digests;
        }

        public CadesManifest(ASiCManifestType asiCManifestType) : base(ManifestSpec.Cades)
        {
            this._asiCManifestType = asiCManifestType ?? throw new ArgumentNullException(nameof(asiCManifestType));
            Digests = this._asiCManifestType?.DataObjectReference?
                .ToImmutableDictionary(
                    d => d.URI,
                    d => new DeclaredDigestFile(
                        d.DigestValue,
                        MessageDigestAlgorithm.FromUri(new Uri(d.DigestMethod.Algorithm)),
                        d.URI,
                        MimeType.ForString(d.MimeType)));
            RootFile = asiCManifestType.DataObjectReference?.Where(d => d.Rootfile).Select(d => d.URI).FirstOrDefault();
        }
    }
}