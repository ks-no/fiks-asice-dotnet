using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KS.Fiks.ASiC_E.Crypto;
using KS.Fiks.ASiC_E.Model;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace KS.Fiks.ASiC_E.Sign
{
    public class XadesSignatureCreator : ISignatureCreator
    {
        public static XadesSignatureCreator Create(
            ICertificateHolder certificateHolder)
        {
            return new XadesSignatureCreator(certificateHolder);
        }

        // Misc:
        private const string Iso8601DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffZ";
        private const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";

        // URIs that denote specific standardized algorithms:
        private const string AlgoXmlEncSha256 = "http://www.w3.org/2001/04/xmlenc#sha256";
        private const string AlgoXmlC14nIncl = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        private const string AlgoXmlDsigMoreRsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private const string AlgoXmlDsigSha1 = "http://www.w3.org/2000/09/xmldsig#sha1";

        // Algorithm names that BouncyCastle accepts in certain method calls:
        private const string AlgoSha1 = "SHA-1";
        private const string AlgoSha256 = "SHA-256";
        private const string AlgoSha256WithRsa = "SHA256withRSA";

        // DS tag inventory that is used:
        private const string TagC14nMethod = "CanonicalizationMethod";
        private const string TagSignedInfo = "SignedInfo";
        private const string TagDigestMethod = "DigestMethod";
        private const string TagDigestValue = "DigestValue";
        private const string TagSignatureMethod = "SignatureMethod";
        private const string TagKeyInfo = "KeyInfo";
        private const string TagObject = "Object";
        private const string TagReference = "Reference";
        private const string TagSignature = "Signature";
        private const string TagSignatureValue = "SignatureValue";
        private const string TagX509Data = "X509Data";
        private const string TagX509Certificate = "X509Certificate";
        private const string TagX509IssuerName = "X509IssuerName";
        private const string TagX509SerialNumber = "X509SerialNumber";
        private const string TagTransform = "Transform";
        private const string TagTransforms = "Transforms";

        // XAdES tag inventory that is used:
        private const string TagCert = "Cert";
        private const string TagCertDigest = "CertDigest";
        private const string TagDataObjectFormat = "DataObjectFormat";
        private const string TagIssuerSerial = "IssuerSerial";
        private const string TagMimeType = "MimeType";
        private const string TagQualifyingProperties = "QualifyingProperties";
        private const string TagSignedDataObjectProperties = "SignedDataObjectProperties";
        private const string TagSignedProperties = "SignedProperties";
        private const string TagSignedSignatureProperties = "SignedSignatureProperties";
        private const string TagSigningCertificate = "SigningCertificate";
        private const string TagSigningTime = "SigningTime";
        private const string TagXadesSignatures = "XAdESSignatures";

        // Attribute names that are used:
        private const string AttrAlgorithm = "Algorithm";
        private const string AttrId = "Id";
        private const string AttrObjectReference = "ObjectReference";
        private const string AttrTarget = "Target";
        private const string AttrType = "Type";
        private const string AttrURI = "URI";

        // Fragment identifiers that are used:
        private const string FragmentIdentSignature = "Signature";
        private const string FragmentIdentSignedProperties = "SignedProperties";

        // XML namespace prefixes that will be used outside of the
        // QualifyingProperties element:
        private const string Etsi121 = ""; // default in root element
        private const string Dsig = "ds"; // default in only child of root element

        // XML namespaces prefixes used at or below the
        // QualifyingProperties element in the document tree.
        private const string NestedEtsi132 = ""; // default in subtree
        private const string NestedDsig = "dsig";

        // XML attribute name for specifying default namespace:
        private const string Xmlns = "xmlns";

        // XML namespace prefix-to-uri mapping for the root element
        // and the first-level Signature element, although Signature,
        // which is a direct child of the root, resets the default
        // namespace from Etsi121 to Dsig:
        private static readonly Dictionary<string, XNamespace> Nsmap = new()
        {
            [Etsi121] = "http://uri.etsi.org/2918/v1.2.1#", // default in root, only used for root
            [Dsig] = "http://www.w3.org/2000/09/xmldsig#", // default in Signature, the only child of root
        };

        // XML namespace prefix-to-uri mapping for elements
        // starting at the QualifyingProperties element:
        private static readonly Dictionary<string, XNamespace> NestedNsmap = new()
        {
            [NestedEtsi132] = "http://uri.etsi.org/01903/v1.3.2#", // default in QualifyingProperties
            [NestedDsig] = "http://www.w3.org/2000/09/xmldsig#",
        };

        private readonly ICertificateHolder _certificateHolder;

        private static byte[] MakeHash(byte[] bytes, string algorithm)
        {
            IDigest digest = DigestUtilities.GetDigest(algorithm);
            digest.BlockUpdate(bytes, 0, bytes.Length);
            byte[] hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);
            return hash;
        }

        private static string SignXml(
            XElement signedInfoElement,
            AsymmetricKeyParameter asymKeyParam)
        {
            var c14n = CanonicalizeSubtree(signedInfoElement);

            // Sign it using BouncyCastle
            var signer = new Asn1SignatureFactory(
                AlgoSha256WithRsa,
                asymKeyParam);

            var sigGen = signer.CreateCalculator();
            sigGen.Stream.Write(c14n, 0, c14n.Length);
            sigGen.Stream.Flush();
            byte[] signature = ((IBlockResult)sigGen.GetResult()).Collect();

            return Convert.ToBase64String(signature);
        }

        // This works around a limitation in a Norwegian DPI vendor, Digipost, where
        // RDN (relative distinguised name) for the organization identifier needs to
        // be written using the older "OID.2.5.4.97=<value>" syntax rather than the
        // newer "organizationIdentifier=<value>" syntax.
        private static string FindCertificateIssuerName(X509Certificate pubCert)
        {
            X509Name issuer = pubCert.IssuerDN;
            IList<DerObjectIdentifier> oids = issuer.GetOidList();
            IList<string> values = issuer.GetValueList();

            var newOids = new List<DerObjectIdentifier>();
            var newValues = new List<string>();

            string oidStr = "2.5.4.97";

            for (int i = 0; i < oids.Count; i++)
            {
                var oid = oids[i];
                var val = values[i];

                // If the OID is 2.5.4.97 (meaning organizationIdentifier),
                // make sure it is written as "OID.2.5.4.97=<value>" instead
                // of "organizationIdentifier=<value>"
                if (oid.Id == oidStr)
                {
                    newOids.Add(new DerObjectIdentifier(oidStr));
                    newValues.Add(val);
                }
                else
                {
                    newOids.Add(oid);
                    newValues.Add(val);
                }
            }

            var customName = new X509Name(newOids, newValues);

            string issuerString = customName.ToString(
                X509Name.DefaultReverse,
                new Dictionary<DerObjectIdentifier, string>());

            return issuerString;
        }

        private static byte[] CanonicalizeSubtree(XNode node)
        {
            var xmlDoc = new XmlDocument();

            using (var reader = node.CreateReader())
            {
                xmlDoc.Load(reader);
            }

            var transform = new System.Security.Cryptography.Xml.XmlDsigC14NTransform();
            transform.LoadInput(xmlDoc);

            using var stream = (Stream)transform.GetOutput(typeof(Stream));
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private static string MakeFragmentID(string id)
            => "#" + id;

        private static XDocument CreateXadesDocument(
            AsymmetricKeyParameter privateKey,
            IEnumerable<string> publicKeys,
            string publicCertificateHash,
            string issuerStr,
            string serialNumberStr,
            IEnumerable<AsicePackageEntry> entries,
            AsicePackageEntry manifestEntry,
            DateTime timeOfSigning)
        {
            string signingTimestamp = timeOfSigning.ToString(
                Iso8601DateTimeFormat,
                CultureInfo.InvariantCulture);

            // Specific StyleCop rules are disabled for the area where the
            // XML tree for the XAdES signature XML file is created using
            // System.Linq.Xml in order to benefit from the ability in that
            // library to create XML with deeply nested constructor calls
            // that directly reflect the nesting structure of the XML itself.
            // Also, the end-parenthesis characters are placed on their own
            // line, with a comment about which node it terminates, as this
            // proved useful while building this XML tree.
            #pragma warning disable SA1009, SA1111, SA1115, SA1116, SA1117, SA1118

            var signedProps = new XElement(NestedNsmap[NestedEtsi132] + TagSignedProperties,
                new XAttribute(Xmlns, NestedNsmap[NestedEtsi132]),
                new XAttribute(XNamespace.Xmlns + NestedDsig, NestedNsmap[NestedDsig]),
                new XAttribute(AttrId, FragmentIdentSignedProperties),
                new XElement(NestedNsmap[NestedEtsi132] + TagSignedSignatureProperties,
                    new XElement(NestedNsmap[NestedEtsi132] + TagSigningTime, signingTimestamp),
                    new XElement(NestedNsmap[NestedEtsi132] + TagSigningCertificate,
                        new XElement(NestedNsmap[NestedEtsi132] + TagCert,
                            new XElement(NestedNsmap[NestedEtsi132] + TagCertDigest,
                                new XElement(NestedNsmap[NestedDsig] + TagDigestMethod,
                                    new XAttribute(AttrAlgorithm, AlgoXmlDsigSha1)),
                                new XElement(NestedNsmap[NestedDsig] + TagDigestValue, publicCertificateHash)
                            ), // CertDigest
                            new XElement(NestedNsmap[NestedEtsi132] + TagIssuerSerial,
                                new XElement(NestedNsmap[NestedDsig] + TagX509IssuerName, issuerStr),
                                new XElement(NestedNsmap[NestedDsig] + TagX509SerialNumber, serialNumberStr)
                            ) // IssuerSerial
                        ) // Cert
                    ) // SigningCertificate
                ), // SignedSignatureProperties
                new XElement(NestedNsmap[NestedEtsi132] + TagSignedDataObjectProperties,
                    entries.Select(entry => new XElement(NestedNsmap[NestedEtsi132] + TagDataObjectFormat,
                        new XAttribute(AttrObjectReference, MakeFragmentID(entry.ID)),
                        new XElement(NestedNsmap[NestedEtsi132] + TagMimeType, entry.Type.ToString())
                    )), // DataObjectFormat and pkgEntries.Select
                    new XElement(NestedNsmap[NestedEtsi132] + TagDataObjectFormat,
                        new XAttribute(AttrObjectReference, MakeFragmentID(manifestEntry.ID)),
                        new XElement(NestedNsmap[NestedEtsi132] + TagMimeType, AsiceConstants.ContentTypeApplicationXml)
                    ) // DataObjectFormat
                ) // SignedDataObjectProperties
            ); // SignedProperties

            byte[] canonicalSignedProps = CanonicalizeSubtree(signedProps);
            byte[] signedPropsHash = MakeHash(canonicalSignedProps, AlgoSha256);
            string signedPropsHashBase64 = Convert.ToBase64String(signedPropsHash);

            var signedInfo = new XElement(Nsmap[Dsig] + TagSignedInfo,
                new XElement(Nsmap[Dsig] + TagC14nMethod,
                    new XAttribute(AttrAlgorithm, AlgoXmlC14nIncl)),
                new XElement(Nsmap[Dsig] + TagSignatureMethod,
                    new XAttribute(AttrAlgorithm, AlgoXmlDsigMoreRsaSha256)),
                entries.Select(pkgEntry => new XElement(Nsmap[Dsig] + TagReference,
                    new XAttribute(AttrId, pkgEntry.ID),
                    new XAttribute(AttrURI, pkgEntry.FileName),
                    new XElement(Nsmap[Dsig] + TagDigestMethod,
                        new XAttribute(AttrAlgorithm, AlgoXmlEncSha256)),
                    new XElement(Nsmap[Dsig] + TagDigestValue,
                        Convert.ToBase64String(pkgEntry.Digest.GetDigest())
                    ) // DigestValue
                )), // Reference and pkgEntries.Select
                new XElement(Nsmap[Dsig] + TagReference,
                    new XAttribute(AttrId, manifestEntry.ID),
                    new XAttribute(AttrURI, manifestEntry.FileName),
                    new XElement(Nsmap[Dsig] + TagDigestMethod,
                        new XAttribute(AttrAlgorithm, AlgoXmlEncSha256)),
                    new XElement(Nsmap[Dsig] + TagDigestValue,
                        Convert.ToBase64String(manifestEntry.Digest.GetDigest())
                    ) // DigestValue
                ), // Reference
                new XElement(Nsmap[Dsig] + TagReference,
                    new XAttribute(AttrType, SignedPropertiesType),
                    new XAttribute(AttrURI, MakeFragmentID(FragmentIdentSignedProperties)),
                    new XElement(Nsmap[Dsig] + TagTransforms,
                        new XElement(Nsmap[Dsig] + TagTransform,
                            new XAttribute(AttrAlgorithm, AlgoXmlC14nIncl)
                        ) // Transform
                    ), // Transforms
                    new XElement(Nsmap[Dsig] + TagDigestMethod,
                        new XAttribute(AttrAlgorithm, AlgoXmlEncSha256)),
                    new XElement(Nsmap[Dsig] + TagDigestValue, signedPropsHashBase64)
                ) // Reference
            ); // SignedInfo

            var signatureValue = SignXml(signedInfo, privateKey);

            return new XDocument(
                new XElement(Nsmap[Etsi121] + TagXadesSignatures,
                    new XAttribute(Xmlns, Nsmap[Etsi121]),
                    new XElement(Nsmap[Dsig] + TagSignature,
                        new XAttribute(Xmlns, Nsmap[Dsig]),
                        new XAttribute(AttrId, FragmentIdentSignature),
                        signedInfo,
                        new XElement(Nsmap[Dsig] + TagSignatureValue, signatureValue),
                        new XElement(Nsmap[Dsig] + TagKeyInfo,
                            new XElement(Nsmap[Dsig] + TagX509Data,
                                publicKeys.Select(key => new XElement(Nsmap[Dsig] + TagX509Certificate,
                                    key
                                )) // X509Certificate and publicKeys.Select
                            ) // X509Data
                        ), // KeyInfo
                        new XElement(Nsmap[Dsig] + TagObject,
                            new XElement(NestedNsmap[NestedEtsi132] + TagQualifyingProperties,
                                new XAttribute(Xmlns, NestedNsmap[NestedEtsi132]),
                                new XAttribute(XNamespace.Xmlns + NestedDsig, NestedNsmap[NestedDsig]),
                                new XAttribute(AttrTarget, MakeFragmentID(FragmentIdentSignature)),
                                signedProps
                            ) // QualifyingProperties
                        ) // Object
                    ) // Signature
                ) // XAdESSignature
            ); // end of document
            #pragma warning restore SA1009, SA1111, SA1116, SA1117, SA1118

            // The returned XML tree contains some redundant XML namespace declarations,
            // which was difficult to avoid given the need to canonicalize and hash a
            // subtree (signedProps) before adding both the hash and the subtree in the
            // final document. The canonicalization requires the namespaces to be present
            // on the subtree, but the QualifyingProperties element also happens to come
            // from the same namespace, so the declarations are repeated between that node
            // and the root node of the subtree.
            //
            // The initial motivation for adding XAdES support has been to send messages
            // to Norway's DPI system, which has two commercial vendors. They both
            // rejected the signature in all attempts to simplify the namespace
            // declarations.
        }

        public XadesSignatureCreator(
            ICertificateHolder certificateHolder)
        {
            _certificateHolder = certificateHolder ??
                throw new ArgumentNullException(nameof(certificateHolder));
        }

        public SignatureFileContainer CreateSignatureFile(
            ManifestContainer manifestContainer,
            IEnumerable<AsicePackageEntry> asicPackageEntries)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = false,
            };

            var pubCert = _certificateHolder.GetPublicCertificate();
            IReadOnlyList<X509Certificate> chain = _certificateHolder.GetCertificateChain();

            byte[] pubCertDer = pubCert.GetEncoded();
            string pubCertStr = Convert.ToBase64String(pubCertDer);

            var completeChain = new List<string> { pubCertStr };
            completeChain.AddRange(
                chain.Select(
                    cert => Convert.ToBase64String(
                        cert.GetEncoded())));

            byte[] pucCertHash = MakeHash(pubCertDer, AlgoSha1);
            string certHash = Convert.ToBase64String(pucCertHash);
            string issuerStr = FindCertificateIssuerName(pubCert);
            BigInteger serialNumber = pubCert.SerialNumber;
            string serialNumberStr = serialNumber.ToString();

            XDocument doc = CreateXadesDocument(
                _certificateHolder.GetPrivateKey(),
                completeChain,
                certHash,
                issuerStr,
                serialNumberStr,
                asicPackageEntries,
                manifestContainer.PackageEntry,
                DateTime.UtcNow);

            byte[] bytes;
            using (var memStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memStream, settings))
                {
                    doc.Save(writer);
                }

                bytes = memStream.ToArray();
            }

            return new SignatureFileContainer(
                manifestContainer.SignatureFileRef,
                bytes);
        }
    }
}
