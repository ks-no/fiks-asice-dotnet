using System.Diagnostics.CodeAnalysis;

namespace KS.Fiks.ASiC_E.Test;

/// <summary>
/// The name and content of an entry in a zip, as read and written by <see cref="TestDataUtil"/>.
/// </summary>
public sealed class ZipEntry
{
    public ZipEntry(string name, byte[] data)
    {
        Name = name;
        Data = data;
    }

    public string Name { get; }

    [SuppressMessage("Performance", "CA1819", Justification = "Test data that is written to a zip as it is")]
    public byte[] Data { get; }
}
