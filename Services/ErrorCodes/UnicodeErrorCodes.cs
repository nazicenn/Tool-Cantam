namespace tool_cantam.Services.ErrorCodes;

public static class UnicodeErrorCodes
{
    public const string InvalidEscape = "L.Unicode.Error.InvalidEscape";
    public const string InvalidCodePoint = "L.Unicode.Error.InvalidCodePoint";
    public const string LoneSurrogate = "L.Unicode.Error.LoneSurrogate";
    public const string NotUtf8 = "L.Unicode.Error.NotUtf8";
    public const string InvalidUnicode = "L.Unicode.Error.InvalidUnicode";
    public const string InputTooLarge = "L.Unicode.Error.InputTooLarge";
    public const string OutputTooLarge = "L.Unicode.Error.OutputTooLarge";
    public const string Unexpected = "L.Common.Error.Unexpected";
}