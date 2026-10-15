using System;
using System.Linq;

namespace KS.Fiks.ASiC_E.Model;

internal static class EntryPath
{
    /// <summary>
    /// A valid path is relative, uses '/' as separator and has no empty, '.' or '..' segments.
    /// A trailing '/' is allowed, as zip uses it for folder entries.
    /// </summary>
    public static bool IsValid(string path)
    {
        if (string.IsNullOrEmpty(path) || path.IndexOf('\\') >= 0 || HasDriveLetter(path))
        {
            return false;
        }

        var segments = (IsFolder(path) ? path.Substring(0, path.Length - 1) : path).Split('/');
        return segments.All(segment => segment.Length > 0 && segment != "." && segment != "..");
    }

    public static bool IsFolder(string path)
    {
        return path.EndsWith("/", StringComparison.Ordinal);
    }

    private static bool HasDriveLetter(string path)
    {
        return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
    }
}
