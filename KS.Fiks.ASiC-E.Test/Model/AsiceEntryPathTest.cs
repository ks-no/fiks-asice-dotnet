using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using KS.Fiks.ASiC_E.Model;
using Shouldly;
using Xunit;

namespace KS.Fiks.ASiC_E.Test.Model;

public class AsiceEntryPathTest
{
    // Windows keeps paths under 260 characters unless long paths are enabled. macOS keeps them under 1024
    private static readonly int MaxFullPathLength = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 240 : 1000;

    private static readonly string LongestSegment = new string('a', AsiceEntryPath.MaxSegmentBytes);

    // 16 segments of 255 bytes and 15 separators: 4095 bytes
    private static readonly string LongestPath = string.Join("/", Enumerable.Repeat(LongestSegment, 16));

    private static readonly string[] Corpus =
    {
        "file.pdf", "root/someFolder/file.pdf", "root/", "~/file.pdf", "~", "$HOME/file.pdf", "a;b`c$d.pdf",
        "Søknad om støtte.pdf", "Sø\u0308knad.pdf", "😀.pdf", " leading space.pdf", "-rf", "..a/b", "a..b",
        "META-INF/ASiCManifest.xml", "META-INF/signature-3f1c.p7s", "mimetype", "docs/mimetype",
        "CONSOLE.txt", "con.pdf", "CON", "COM1.tar.gz", "lpt9", "COM0.log", "COM\u00b9.txt", "root/NUL/file.pdf", "CON .txt", "NUL.", "NUL",
        string.Empty, "/", "/file.pdf", "//server/share/file.pdf", "C:/file.pdf", "c:file.pdf", "a:b", "ab:c",
        "file.pdf:hidden", "root/C:/file.pdf", "root\\file.pdf", "root\\..\\file.pdf", "root//file.pdf", "root//",
        "./file.pdf", "../file.pdf", "root/../../file.pdf", "../", "./", "META-INF/../file.pdf", ".", "..", "...",
        "file\u0000.pdf", "file\n.pdf", "file\u007f.pdf", "invoice\u0085FAKE.pdf", "a\u2028b.pdf", "a\u2029b.pdf",
        "\ud800.pdf", "evil\u202efdp.exe", "a\u2066b.pdf", "a\u200eb.pdf",
        "what?.pdf", "a*b", "a\"b", "a<b", "a>b", "a|b",
        "file.pdf.", "file.pdf ", "root./file.pdf", "root /file.pdf", ". ", ".. ", "x/.. ",
        LongestSegment, LongestSegment + "a", new string('ø', 127), new string('ø', 128),
        LongestPath, "a/" + LongestPath
    };

    // Pieces that random paths are built from: ordinary characters, every separator and special character
    // the rules mention, and a few whole names that matter
    private static readonly string[] Tokens =
    {
        "a", "b", "Z", "0", "9", "æ", "ø", "Å", "é", "e\u0301", " ", ".", "..", "...", "/", "./", "//", "\\", ":",
        "~", "$", "*", "?", "\"", "<", ">", "|", "-", "_", "\u0000", "\u001f", "\u007f", "\u0085", "\u2028", "\u202e",
        "\u200e", "\n", "😀", "\ud800", "CON", "com1", "COM0", "NUL.", "META-INF", "mimetype", "C:", "x.", "x ", "..a"
    };

    [Theory(DisplayName = "The first broken rule is reported, validity rules before portability rules")]
    [InlineData("file.pdf", null)]
    [InlineData("root/someFolder/file.pdf", null)]
    [InlineData("root/", null)]
    [InlineData("~/file.pdf", null)]
    [InlineData("Søknad om støtte.pdf", null)]
    [InlineData(" leading space.pdf", null)]
    [InlineData("-rf", null)]
    [InlineData("a;b`c$d.pdf", null)]
    [InlineData("😀.pdf", null)]
    [InlineData("a\u200eb.pdf", null)]
    [InlineData("..a/b", null)]
    [InlineData("META-INF/ASiCManifest.xml", null)]
    [InlineData("mimetype", null)]
    [InlineData("CONSOLE.txt", null)]
    [InlineData("", EntryPathRule.Empty)]
    [InlineData("/file.pdf", EntryPathRule.Absolute)]
    [InlineData("/", EntryPathRule.Absolute)]
    [InlineData("//server/share/file.pdf", EntryPathRule.Absolute)]
    [InlineData("C:/file.pdf", EntryPathRule.Absolute)]
    [InlineData("c:file.pdf", EntryPathRule.Absolute)]
    [InlineData("a:b", EntryPathRule.Absolute)]
    [InlineData("root\\file.pdf", EntryPathRule.Backslash)]
    [InlineData("root//file.pdf", EntryPathRule.EmptySegment)]
    [InlineData("root//", EntryPathRule.EmptySegment)]
    [InlineData("./file.pdf", EntryPathRule.DotSegment)]
    [InlineData("../file.pdf", EntryPathRule.DotSegment)]
    [InlineData("root/../../file.pdf", EntryPathRule.DotSegment)]
    [InlineData("../", EntryPathRule.DotSegment)]
    [InlineData("file\u0000.pdf", EntryPathRule.ControlCharacter)]
    [InlineData("file\n.pdf", EntryPathRule.ControlCharacter)]
    [InlineData("file\u007f.pdf", EntryPathRule.ControlCharacter)]
    [InlineData("invoice\u0085FAKE.pdf", EntryPathRule.ControlCharacter)]
    [InlineData("a\u2028b.pdf", EntryPathRule.ControlCharacter)]
    [InlineData("a\u2029b.pdf", EntryPathRule.ControlCharacter)]
    [InlineData("evil\u202efdp.exe", EntryPathRule.BidiControl)]
    [InlineData("a\u2066b.pdf", EntryPathRule.BidiControl)]
    [InlineData("ab:c", EntryPathRule.ForbiddenCharacter)]
    [InlineData("file.pdf:hidden", EntryPathRule.ForbiddenCharacter)]
    [InlineData("root/C:/file.pdf", EntryPathRule.ForbiddenCharacter)]
    [InlineData("what?.pdf", EntryPathRule.ForbiddenCharacter)]
    [InlineData("a*b", EntryPathRule.ForbiddenCharacter)]
    [InlineData("a\"b", EntryPathRule.ForbiddenCharacter)]
    [InlineData("a<b", EntryPathRule.ForbiddenCharacter)]
    [InlineData("a>b", EntryPathRule.ForbiddenCharacter)]
    [InlineData("a|b", EntryPathRule.ForbiddenCharacter)]
    [InlineData("file.pdf.", EntryPathRule.TrailingDotOrSpace)]
    [InlineData("file.pdf ", EntryPathRule.TrailingDotOrSpace)]
    [InlineData("root./file.pdf", EntryPathRule.TrailingDotOrSpace)]
    [InlineData("...", EntryPathRule.TrailingDotOrSpace)]
    [InlineData("CON", EntryPathRule.ReservedDeviceName)]
    [InlineData("con.pdf", EntryPathRule.ReservedDeviceName)]
    [InlineData("COM1.tar.gz", EntryPathRule.ReservedDeviceName)]
    [InlineData("lpt9", EntryPathRule.ReservedDeviceName)]
    [InlineData("COM0.log", EntryPathRule.ReservedDeviceName)]
    [InlineData("lpt0", EntryPathRule.ReservedDeviceName)]
    [InlineData("COM\u00b9.txt", EntryPathRule.ReservedDeviceName)]
    [InlineData("root/NUL/file.pdf", EntryPathRule.ReservedDeviceName)]
    [InlineData("CON .txt", EntryPathRule.ReservedDeviceName)]
    [InlineData("NUL.", EntryPathRule.ReservedDeviceName)]
    [InlineData("con.pdf.", EntryPathRule.ReservedDeviceName)]
    [InlineData("folder/NUL:", EntryPathRule.ReservedDeviceName)]
    [InlineData("NUL:stream", EntryPathRule.ReservedDeviceName)]
    [InlineData("COM1:", EntryPathRule.ReservedDeviceName)]
    [InlineData("con :x", EntryPathRule.ReservedDeviceName)]
    [InlineData("evil\u202e/con.pdf", EntryPathRule.BidiControl)]
    [InlineData("ab:c/evil\u202efdp.exe", EntryPathRule.BidiControl)]
    [InlineData("a:b/../c", EntryPathRule.Absolute)]
    [InlineData("ab:c/../d", EntryPathRule.DotSegment)]
    [InlineData("con.pdf/../c", EntryPathRule.DotSegment)]
    [InlineData("evil\u202efdp.exe/../c", EntryPathRule.DotSegment)]
    public void FirstViolationIsReported(string path, EntryPathRule? expected)
    {
        AsiceEntryPath.FirstViolation(path).ShouldBe(expected);
        AsiceEntryPath.IsPortable(path).ShouldBe(expected == null);
        AsiceEntryPath.IsValid(path).ShouldBe(expected == null || AsiceEntryPath.IsPortabilityRule(expected.Value));
        (AsiceEntryPath.Describe(path) == null).ShouldBe(expected == null);
    }

    [Fact(DisplayName = "A lone surrogate is rejected")]
    public void LoneSurrogateIsRejected()
    {
        // Not in the table above: xunit cannot carry a lone surrogate through InlineData
        AsiceEntryPath.FirstViolation("\ud800.pdf").ShouldBe(EntryPathRule.LoneSurrogate);
        AsiceEntryPath.FirstViolation("root/\udc00/file.pdf").ShouldBe(EntryPathRule.LoneSurrogate);
        AsiceEntryPath.FirstViolation("\ud83d\ude00.pdf").ShouldBeNull();
        AsiceEntryPath.Describe("\ud800.pdf").ShouldContain("lone surrogate");
    }

    [Fact(DisplayName = "A segment over 255 bytes in UTF-8 is valid but not portable")]
    public void SegmentLengthIsCountedInUtf8Bytes()
    {
        AsiceEntryPath.FirstViolation(LongestSegment).ShouldBeNull();
        AsiceEntryPath.FirstViolation(LongestSegment + "a").ShouldBe(EntryPathRule.SegmentTooLong);
        AsiceEntryPath.IsValid(LongestSegment + "a").ShouldBeTrue();
        AsiceEntryPath.FirstViolation(new string('ø', 127)).ShouldBeNull();
        AsiceEntryPath.FirstViolation(new string('ø', 128)).ShouldBe(EntryPathRule.SegmentTooLong);
        AsiceEntryPath.FirstViolation("root/" + LongestSegment + "a").ShouldBe(EntryPathRule.SegmentTooLong);
    }

    [Fact(DisplayName = "A path over 4096 bytes in UTF-8 is valid but not portable")]
    public void PathLengthIsCountedInUtf8Bytes()
    {
        AsiceEntryPath.FirstViolation(LongestPath).ShouldBeNull();
        AsiceEntryPath.FirstViolation("a/" + LongestPath).ShouldBe(EntryPathRule.TooLong);
        AsiceEntryPath.IsValid("a/" + LongestPath).ShouldBeTrue();
    }

    [Fact(DisplayName = "Describe and Escape write characters that cannot be printed as U+XXXX")]
    public void DescribeEscapesControlCharacters()
    {
        var description = AsiceEntryPath.Describe("file\n.pdf");
        description.ShouldContain("U+000A");
        description.ShouldNotContain("\n");

        AsiceEntryPath.Describe("invoice\u0085FAKE.pdf").ShouldContain("U+0085");
        AsiceEntryPath.Describe("evil\u202efdp.exe").ShouldContain("U+202E");
        AsiceEntryPath.Describe("evil\u202efdp.exe").ShouldNotContain("\u202e");
        AsiceEntryPath.Describe("file.pdf:hidden").ShouldContain("':' (U+003A)");
        AsiceEntryPath.Describe("file.pdf").ShouldBeNull();

        AsiceEntryPath.Escape("a\u0085b\u2028c\u202ed\ud800e😀").ShouldBe("aU+0085bU+2028cU+202EdU+D800e😀");
        AsiceEntryPath.Escape(null).ShouldBeNull();
    }

    [Fact(DisplayName = "No valid path leaves the directory it is combined with, and every portable path is stored under its own name")]
    public void ValidPathsStayInsideTheDirectory()
    {
        var baseDirectory = Path.Combine(
            Path.GetTempPath(),
            "asice-entry-path-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(baseDirectory);
        var failures = new List<string>();
        try
        {
            var index = 0;
            foreach (var path in Corpus.Concat(RandomPaths(1000)))
            {
                // A directory of its own for each path, so that a file created for one path cannot be in
                // the way of a folder with the same name for the next
                var directory = Path.Combine(baseDirectory, index.ToString(CultureInfo.InvariantCulture));
                index++;

                var failure = CheckProperties(directory, path);
                if (failure != null)
                {
                    failures.Add($"'{AsiceEntryPath.Escape(path)}': {failure}");
                }
            }
        }
        finally
        {
            TryDelete(baseDirectory);
        }

        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// The properties every path must have, whatever the rules say:
    /// a valid path combined with a directory stays inside it;
    /// a portable path is not changed by the operating system and can be created and found under its own name;
    /// a path rejected for escaping or being rewritten really is moved or rewritten by the operating system.
    /// </summary>
    private static string CheckProperties(string directory, string path)
    {
        var rule = AsiceEntryPath.FirstViolation(path);
        var valid = AsiceEntryPath.IsValid(path);
        var portable = AsiceEntryPath.IsPortable(path);

        string full = null;
        var contained = false;
        var faithful = false;
        try
        {
            full = Path.GetFullPath(Path.Combine(directory, path));
            contained = full.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            if (contained)
            {
                var relative = Path.GetRelativePath(directory, full).Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');
                if (relative == ".")
                {
                    relative = string.Empty;
                }

                var expected = AsiceEntryPath.IsFolder(path) ? path.Substring(0, path.Length - 1) : path;
                faithful = relative == expected;
            }
        }
        catch (ArgumentException)
        {
            // Characters the operating system does not allow in a path, such as U+0000
        }
        catch (PathTooLongException)
        {
        }
        catch (NotSupportedException)
        {
        }

        if (valid && !contained)
        {
            return $"valid, but combined with a directory it becomes '{full}', which is outside the directory";
        }

        if (portable && !faithful)
        {
            return $"portable, but the operating system changed it to '{full}'";
        }

        if (portable && !AsiceEntryPath.IsFolder(path) && full.Length < MaxFullPathLength)
        {
            var writeFailure = TryWriteAndReadBack(full, path);
            if (writeFailure != null)
            {
                return writeFailure;
            }
        }

        if (!valid && MustBeDemonstrable(rule.Value, path) && contained && faithful)
        {
            return $"rejected as {rule}, but combined with a directory it stays inside and unchanged";
        }

        return null;
    }

    private static string TryWriteAndReadBack(string full, string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(full);
            Directory.CreateDirectory(directory);
            using (File.Create(full))
            {
            }

            var lastSegment = path.Substring(path.LastIndexOf('/') + 1);
            var stored = Directory.EnumerateFiles(directory).Select(Path.GetFileName).Any(name => name == lastSegment);
            File.Delete(full);
            return stored ? null : "portable, but the file system stored it under another name";
        }
        catch (Exception exception) when (exception is IOException
                                          || exception is UnauthorizedAccessException
                                          || exception is ArgumentException
                                          || exception is NotSupportedException)
        {
            return $"portable, but the file could not be created: {exception.GetType().Name}: {exception.Message}";
        }
    }

    /// <summary>
    /// The rules whose effect the operating system the test runs on can show. The other rules reject paths
    /// that fail in other ways, such as containing characters the file system refuses.
    /// </summary>
    private static bool MustBeDemonstrable(EntryPathRule rule, string path)
    {
        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        switch (rule)
        {
            case EntryPathRule.EmptySegment:
            case EntryPathRule.DotSegment:
                return true;
            case EntryPathRule.Absolute:
                return windows || path.StartsWith("/", StringComparison.Ordinal);
            case EntryPathRule.Backslash:
                return windows;
            default:
                return false;
        }
    }

    private static IEnumerable<string> RandomPaths(int count)
    {
        // A fixed seed, so the same paths are tested every time
        var random = new Random(20261015);
        for (var i = 0; i < count; i++)
        {
            var tokenCount = random.Next(0, 13);
            var builder = new StringBuilder();
            for (var t = 0; t < tokenCount; t++)
            {
                builder.Append(Tokens[random.Next(Tokens.Length)]);
            }

            yield return builder.ToString();
        }
    }

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
