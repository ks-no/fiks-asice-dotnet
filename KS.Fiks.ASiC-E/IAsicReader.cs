using System.IO;
using KS.Fiks.ASiC_E.Model;

namespace KS.Fiks.ASiC_E
{
    public interface IAsicReader
    {
        AsiceReadModel Read(Stream inputStream);
    }
}