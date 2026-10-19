using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KS.Fiks.ASiC_E.Model;

/// <summary>
/// The rules for the path of an entry in an ASiC-E package. ETSI EN 319 162-1 requires the package to follow
/// the ZIP specification, and section 4.4.17.1 of the specification (APPNOTE) says that the path must not have
/// a drive letter or a leading '/', and that the separator is '/'. In addition the path must not have empty,
/// '.' or '..' segments, so that it stays inside the folder the package is unpacked to.
/// </summary>
internal static class EntryPath
{
    /// <summary>
    /// True if the path is relative, uses '/' as separator and has no empty, '.' or '..' segments.
    /// A trailing '/' is allowed, as zip uses it for folder entries.
    /// </summary>
    public static bool IsValid(string path)
    {
        if (string.IsNullOrEmpty(path) || path.IndexOf("\\", StringComparison.Ordinal) >= 0 || HasDriveLetter(path))
        {
            return false;
        }

        var segments = (IsFolder(path) ? path.Substring(0, path.Length - 1) : path).Split('/');
        return segments.All(segment => segment.Length > 0 && segment != "." && segment != "..");
    }

    /// <summary>True if the path ends with '/', which zip uses for folder entries.</summary>
    public static bool IsFolder(string path)
    {
        return path != null && path.EndsWith("/", StringComparison.Ordinal);
    }

    /// <summary>The name of the file, without the folders it is in.</summary>
    public static string FileName(string path)
    {
        return path.Substring(path.LastIndexOf("/", StringComparison.Ordinal) + 1);
    }

    /// <summary>
    /// True if files with these paths, which have the same file name ignoring case, cannot be told apart by
    /// their file name. Names that differ only in case are allowed in the root folder, as they always have been,
    /// but not when folders are involved, and the same path is never allowed twice.
    /// </summary>
    public static bool IsFileNameConflict(IReadOnlyCollection<string> paths)
    {
        return paths.Count > 1
               && (paths.Any(path => path.IndexOf("/", StringComparison.Ordinal) >= 0)
                   || paths.Distinct(StringComparer.Ordinal).Count() < paths.Count);
    }

    /// <summary>
    /// Writes control characters, line separators, bidirectional control characters and lone surrogates as
    /// U+XXXX, so that a name from a package cannot put line breaks into a log or change how an exception
    /// message is displayed.
    /// </summary>
    public static string Escape(string text)
    {
        if (text == null)
        {
            return null;
        }

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (char.IsHighSurrogate(character) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                builder.Append(character).Append(text[i + 1]);
                i++;
            }
            else if (IsControl(character) || IsBidiControl(character) || char.IsSurrogate(character))
            {
                builder.Append("U+").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool HasDriveLetter(string path)
    {
        return path.Length >= 2 && IsAsciiLetter(path[0]) && path[1] == ':';
    }

    private static bool IsAsciiLetter(char character)
    {
        return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
    }

    private static bool IsControl(char character)
    {
        return char.IsControl(character) || character == '\u2028' || character == '\u2029';
    }

    private static bool IsBidiControl(char character)
    {
        return (character >= '\u202a' && character <= '\u202e') || (character >= '\u2066' && character <= '\u2069');
    }
}
