using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E.Manifest
{
    public class CadesManifestReader : IManifestReader<CadesManifest>
    {
        public const string ErrorMessageInvalidNamespace =
            "Could not parse manifest, check that all namespaces are correct";

        public CadesManifest FromStream(Stream stream)
        {
            using (var textReader = NormalizeNamespaces(stream))
            using (var xmlReader = XmlReader.Create(textReader))
            {
                try
                {
                    var manifest =
                        (ASiCManifestType)new XmlSerializer(typeof(ASiCManifestType)).Deserialize(xmlReader);
                    return new CadesManifest(manifest);
                }
                catch (InvalidOperationException e)
                {
                    throw new ManifestReadException(ErrorMessageInvalidNamespace, e);
                }
            }
        }

        private static TextReader NormalizeNamespaces(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, true))
            {
                var xmlString = streamReader.ReadToEnd()
                    .Replace(
                        "http://uri.etsi.org/02918/v1.2.1#",
                        Namespaces.CadesAsicNamespace,
                        StringComparison.CurrentCultureIgnoreCase)
                    .Replace(
                        "http://uri.etsi.org/02918/v1.1.1#",
                        Namespaces.CadesAsicNamespace,
                        StringComparison.CurrentCultureIgnoreCase);
                return new StringReader(xmlString);
            }
        }
    }
}