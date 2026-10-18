namespace tool_cantam.Services.ErrorCodes;

public static class Base64ErrorCodes
{
    public const string InvalidCharacter = "L.Base64.Error.InvalidCharacter";
    public const string InvalidLength = "L.Base64.Error.InvalidLength";
    public const string InvalidPadding = "L.Base64.Error.InvalidPadding";
    public const string InvalidFormat = "L.Base64.Error.InvalidFormat";
    public const string NotUtf8Text = "L.Base64.Error.NotUtf8Text";
    public const string InputTooLarge = "L.Base64.Error.InputTooLarge";
    public const string OutputTooLarge = "L.Base64.Error.OutputTooLarge";
    public const string PathRequired = "L.Base64.Error.PathRequired";
    public const string FileNotFound = "L.Base64.Error.FileNotFound";
    public const string InvalidFileContent = "L.Base64.Error.InvalidFileContent";
    public const string IoError = "L.Base64.Error.IoError";
    public const string Unexpected = "L.Common.Error.Unexpected";
}