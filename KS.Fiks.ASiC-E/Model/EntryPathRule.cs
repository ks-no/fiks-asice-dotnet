namespace KS.Fiks.ASiC_E.Model;

/// <summary>
/// A rule that the path of an entry in an ASiC-E package can break. See <see cref="AsiceEntryPath"/>.
/// The rules up to and including <see cref="ReservedDeviceName"/> decide whether a path is valid. The rules
/// after that decide whether a valid path is also portable. <see cref="AsiceEntryPath.IsPortabilityRule"/>
/// relies on this order.
/// </summary>
public enum EntryPathRule
{
    /// <summary>The path is empty.</summary>
    Empty,

    /// <summary>The path contains a lone surrogate character and cannot be encoded as UTF-8.</summary>
    LoneSurrogate,

    /// <summary>The path starts with '/' or with a drive letter, so it is not relative to the root of the package.</summary>
    Absolute,

    /// <summary>The path contains '\'. The separator is '/'.</summary>
    Backslash,

    /// <summary>A segment of the path is empty, as in <c>a//b</c>.</summary>
    EmptySegment,

    /// <summary>A segment of the path is '.' or '..'.</summary>
    DotSegment,

    /// <summary>
    /// A segment contains a control character (U+0000 to U+001F, U+007F to U+009F) or a line or paragraph
    /// separator (U+2028, U+2029).
    /// </summary>
    ControlCharacter,

    /// <summary>
    /// A segment contains a bidirectional control character (U+202A to U+202E, U+2066 to U+2069), which can
    /// change how the name is displayed, so that <c>evil\u202Efdp.exe</c> looks like <c>evilexe.pdf</c>.
    /// </summary>
    BidiControl,

    /// <summary>
    /// A segment is a reserved device name on Windows (CON, PRN, AUX, NUL, COM0 to COM9, LPT0 to LPT9, and
    /// the forms with superscript digits), alone or followed by an extension or a colon, as in <c>NUL.txt</c>,
    /// <c>NUL:</c> and <c>NUL:stream</c>. Windows redirects such a name to the device, whatever directory it is
    /// combined with.
    /// </summary>
    ReservedDeviceName,

    /// <summary>
    /// A segment contains one of <c>: * ? " &lt; &gt; |</c>, which Windows does not allow in a file name.
    /// Portability rule.
    /// </summary>
    ForbiddenCharacter,

    /// <summary>A segment ends with '.' or ' ', which Windows removes from a file name. Portability rule.</summary>
    TrailingDotOrSpace,

    /// <summary>
    /// A segment is longer than <see cref="AsiceEntryPath.MaxSegmentBytes"/> bytes in UTF-8, the limit of
    /// common file systems. Portability rule.
    /// </summary>
    SegmentTooLong,

    /// <summary>
    /// The path is longer than <see cref="AsiceEntryPath.MaxPathBytes"/> bytes in UTF-8. Portability rule.
    /// </summary>
    TooLong
}
