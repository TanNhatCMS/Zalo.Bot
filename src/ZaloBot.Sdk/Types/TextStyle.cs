namespace ZaloBot.Sdk.Types;

/// <summary>One explicit rich-text style run of a sendMessage <c>text_styles</c> entry
/// (docs bot.zapps.me/docs/apis/sendMessage). Offsets are UTF-16 code units — the same unit
/// as .NET string indexing, so <c>text.IndexOf(...)</c>/<c>string.Length</c> values apply directly.</summary>
public class TextStyle
{
    /// <summary>Start offset of the styled range, in UTF-16 code units (0-based).
    /// Always serialized — offset 0 is valid and must not be dropped by WhenWritingDefault.</summary>
    [JsonPropertyName("start")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int Start { get; set; }

    /// <summary>Length of the styled range, in UTF-16 code units</summary>
    [JsonPropertyName("len")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int Length { get; set; }

    /// <summary>Style codes applied to the range, e.g. ["b", "c_db342e"] — see <see cref="TextStyleCode"/>.
    /// Unknown codes are ignored by the platform; ranges outside the text are auto-adjusted or dropped.</summary>
    [JsonPropertyName("st")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public IReadOnlyList<string> Styles { get; set; } = [];

    /// <summary>Create a style run.</summary>
    /// <param name="start">Start offset in UTF-16 code units (0-based)</param>
    /// <param name="length">Length in UTF-16 code units</param>
    /// <param name="styles">Style codes, e.g. <c>[TextStyleCode.Bold, TextStyleCode.ColorRed]</c></param>
    public static TextStyle Of(int start, int length, params string[] styles) => new()
    {
        Start = start,
        Length = length,
        Styles = styles,
    };
}

/// <summary>Official style codes accepted in <see cref="TextStyle.Styles"/> (docs bot.zapps.me/docs/apis/sendMessage).</summary>
public static class TextStyleCode
{
    public const string Bold = "b";
    public const string Italic = "i";
    public const string Underline = "u";
    public const string Strikethrough = "s";

    /// <summary>Font sizes: small / normal / large / very large</summary>
    public const string FontSmall = "f_13";
    public const string FontNormal = "f_15";
    public const string FontLarge = "f_18";
    public const string FontVeryLarge = "f_20";

    /// <summary>Text colors from the platform's supported palette</summary>
    public const string ColorDefault = "c_050a19";
    public const string ColorGreen = "c_15a85f";
    public const string ColorYellow = "c_f7b503";
    public const string ColorOrange = "c_f27806";
    public const string ColorRed = "c_db342e";

    /// <summary>List markers: unordered / ordered</summary>
    public const string ListUnordered = "lst_1";
    public const string ListOrdered = "lst_2";

    /// <summary>Indentation levels 1–5</summary>
    public const string Indent1 = "ind_1";
    public const string Indent2 = "ind_2";
    public const string Indent3 = "ind_3";
    public const string Indent4 = "ind_4";
    public const string Indent5 = "ind_5";
}
