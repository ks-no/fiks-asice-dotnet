using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KS.Fiks.ASiC_E.Model;

/// <summary>
/// The rules for the path of an entry in an ASiC-E package, as read from the zip and as given to
/// <see cref="AsiceBuilder.AddFileWithPath"/>.
/// <para>
/// A <b>valid</b> path is relative, uses '/' as separator, has no empty, '.' or '..' segments, and has no
/// control characters, line separators or lone surrogates. Combined with a directory using
/// <c>Path.Combine</c>, a valid path stays inside that directory. The reader rejects packages with entries that
/// are not valid, and the builder rejects such file names.
/// </para>
/// <para>
/// A <b>portable</b> path is valid and is also a legal file name on Windows, Linux and macOS: none of
/// <c>: * ? " &lt; &gt; |</c>, no segment that ends with '.' or ' ', no reserved device name such as <c>CON</c>
/// or <c>COM1</c>, no bidirectional control characters, and at most <see cref="MaxSegmentBytes"/> bytes per
/// segment and <see cref="MaxPathBytes"/> bytes in total. The reader and the builder accept paths that are
/// valid but not portable, and log a warning, because such names are legal on some systems. Check
/// <see cref="IsPortable"/> before you write a file to disk under a name from a package. On Windows, a reserved
/// device name is redirected to the device whatever directory it is combined with.
/// </para>
/// <para>
/// No rule makes a name safe for a shell: <c>~</c>, <c>$</c>, <c>;</c>, backticks and a leading '-' are allowed.
/// The rules do not look at what already exists at the target, such as symbolic links, do not normalise Unicode,
/// do not detect names that are the same file on a case-insensitive file system, and do not know the limits of
/// the file system you write to, such as the 260 character path limit on Windows.
/// </para>
/// </summary>
public static class AsiceEntryPath
{
    /// <summary>The longest portable segment, in UTF-8 bytes. The limit of NTFS, ext4 and APFS.</summary>
    public const int MaxSegmentBytes = 255;

    /// <summary>The longest portable path, in UTF-8 bytes.</summary>
    public const int MaxPathBytes = 4096;

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private static readonly char[] ForbiddenCharacters = { ':', '*', '?', '"', '<', '>', '|' };

    // From "Naming Files, Paths, and Namespaces" in the Windows documentation
    private static readonly HashSet<string> ReservedDeviceNames = new HashSet<string>(
        new[] { "CON", "PRN", "AUX", "NUL" }
            .Concat(new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "\u00b9", "\u00b2", "\u00b3" }
                .SelectMany(digit => new[] { "COM" + digit, "LPT" + digit })),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True if the path is valid: relative, '/' as separator, no empty, '.' or '..' segments, no control
    /// characters. See the class documentation.
    /// </summary>
    public static bool IsValid(string path)
    {
        var rule = FirstViolation(path);
        return rule == null || IsPortabilityRule(rule.Value);
    }

    /// <summary>
    /// True if the path is valid and every segment is also a legal file name on Windows, Linux and macOS. See
    /// the class documentation.
    /// </summary>
    public static bool IsPortable(string path)
    {
        return FirstViolation(path) == null;
    }

    /// <summary>True if the path ends with '/', which zip uses for folder entries.</summary>
    public static bool IsFolder(string path)
    {
        return path != null && path.EndsWith("/", StringComparison.Ordinal);
    }

    /// <summary>True for the rules that a valid path may break and still be valid, but not portable.</summary>
    public static bool IsPortabilityRule(EntryPathRule rule)
    {
        // The enum declares the validity rules first, see its documentation
        return rule > EntryPathRule.ControlCharacter;
    }

    /// <summary>
    /// The first rule the path breaks, or null if the path is portable. The validity rules are checked before
    /// the portability rules, so a valid path that is not portable gives a portability rule.
    /// </summary>
    public static EntryPathRule? FirstViolation(string path)
    {
        return Analyze(path).Rule;
    }

    /// <summary>
    /// Describes the first rule the path breaks, or returns null if the path is portable. Characters that
    /// cannot be printed are written as U+XXXX, so the text can be logged.
    /// </summary>
    public static string Describe(string path)
    {
        var violation = Analyze(path);
        return violation.Rule == null ? null : DescribeViolation(violation);
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
                builder.Append(CodePoint(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static Violation Analyze(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new Violation(EntryPathRule.Empty);
        }

        int totalBytes;
        try
        {
            totalBytes = StrictUtf8.GetByteCount(path);
        }
        catch (EncoderFallbackException)
        {
            return new Violation(EntryPathRule.LoneSurrogate);
        }

        if (path[0] == '/' || HasDriveLetter(path))
        {
            return new Violation(EntryPathRule.Absolute);
        }

        if (path.IndexOf("\\", StringComparison.Ordinal) >= 0)
        {
            return new Violation(EntryPathRule.Backslash);
        }

        var segments = (IsFolder(path) ? path.Substring(0, path.Length - 1) : path).Split('/');

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                return new Violation(EntryPathRule.EmptySegment);
            }

            if (segment == "." || segment == "..")
            {
                return new Violation(EntryPathRule.DotSegment);
            }

            var controlIndex = IndexOf(segment, IsControl);
            if (controlIndex >= 0)
            {
                return new Violation(EntryPathRule.ControlCharacter, segment, segment[controlIndex]);
            }
        }

        foreach (var segment in segments)
        {
            var bidiIndex = IndexOf(segment, IsBidiControl);
            if (bidiIndex >= 0)
            {
                return new Violation(EntryPathRule.BidiControl, segment, segment[bidiIndex]);
            }

            var forbiddenIndex = segment.IndexOfAny(ForbiddenCharacters);
            if (forbiddenIndex >= 0)
            {
                return new Violation(EntryPathRule.ForbiddenCharacter, segment, segment[forbiddenIndex]);
            }

            var last = segment[segment.Length - 1];
            if (last == '.' || last == ' ')
            {
                return new Violation(EntryPathRule.TrailingDotOrSpace, segment);
            }

            if (IsReservedDeviceName(segment))
            {
                return new Violation(EntryPathRule.ReservedDeviceName, segment);
            }

            if (StrictUtf8.GetByteCount(segment) > MaxSegmentBytes)
            {
                return new Violation(EntryPathRule.SegmentTooLong, segment);
            }
        }

        if (totalBytes > MaxPathBytes)
        {
            return new Violation(EntryPathRule.TooLong);
        }

        return default;
    }

    private static string DescribeViolation(Violation violation)
    {
        var segment = Escape(violation.Segment);
        switch (violation.Rule)
        {
            case EntryPathRule.Empty:
                return "the path is empty";
            case EntryPathRule.LoneSurrogate:
                return "the path contains a lone surrogate character and is not valid UTF-8";
            case EntryPathRule.Absolute:
                return "the path starts with '/' or a drive letter, so it is not relative to the root of the package";
            case EntryPathRule.Backslash:
                return "the path contains '\\'; the separator is '/'";
            case EntryPathRule.EmptySegment:
                return "the path contains an empty segment";
            case EntryPathRule.DotSegment:
                return "the path contains a '.' or '..' segment";
            case EntryPathRule.ControlCharacter:
                return $"the segment '{segment}' contains the control character {CodePoint(violation.Character)}";
            case EntryPathRule.BidiControl:
                return $"the segment '{segment}' contains the bidirectional control character {CodePoint(violation.Character)}, which can change how the name is displayed";
            case EntryPathRule.ForbiddenCharacter:
                return $"the segment '{segment}' contains '{violation.Character}' ({CodePoint(violation.Character)}), which is not allowed in a file name on Windows";
            case EntryPathRule.TrailingDotOrSpace:
                return $"the segment '{segment}' ends with '.' or ' ', which Windows removes from a file name";
            case EntryPathRule.ReservedDeviceName:
                return $"the segment '{segment}' is a reserved device name on Windows";
            case EntryPathRule.SegmentTooLong:
                return $"the segment '{segment}' is longer than {MaxSegmentBytes} bytes";
            case EntryPathRule.TooLong:
                return $"the path is longer than {MaxPathBytes} bytes";
            default:
                return null;
        }
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

    private static int IndexOf(string segment, Func<char, bool> predicate)
    {
        for (var i = 0; i < segment.Length; i++)
        {
            if (predicate(segment[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsReservedDeviceName(string segment)
    {
        var dot = segment.IndexOf(".", StringComparison.Ordinal);
        var stem = (dot < 0 ? segment : segment.Substring(0, dot)).TrimEnd(' ');
        return ReservedDeviceNames.Contains(stem);
    }

    private static string CodePoint(char character)
    {
        return "U+" + ((int)character).ToString("X4", CultureInfo.InvariantCulture);
    }

    private readonly struct Violation
    {
        public Violation(EntryPathRule rule, string segment = null, char character = '\0')
        {
            Rule = rule;
            Segment = segment;
            Character = character;
        }

        public EntryPathRule? Rule { get; }

        public string Segment { get; }

        public char Character { get; }
    }
}
