using System.IO;
using System.Xml;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E.Manifest
{
    public class CadesManifestReader : IManifestReader<CadesManifest>
    {
        public CadesManifest FromStream(Stream stream)
        {
            using (var xmlReader = XmlReader.Create(stream))
            {
                var manifest = (ASiCManifestType)new XmlSerializer(typeof(ASiCManifestType)).Deserialize(xmlReader);
                return new CadesManifest(manifest);
            }
        }
    }
}