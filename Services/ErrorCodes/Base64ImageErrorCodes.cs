namespace tool_cantam.Services.ErrorCodes;

public static class Base64ImageErrorCodes
{
    public const string PathRequired = "L.Base64Image.Error.PathRequired";
    public const string FileNotFound = "L.Base64Image.Error.FileNotFound";
    public const string FileTooLarge = "L.Base64Image.Error.FileTooLarge";
    public const string UnsupportedFormat = "L.Base64Image.Error.UnsupportedFormat";
    public const string InvalidBase64 = "L.Base64Image.Error.InvalidBase64";
    public const string NotAnImage = "L.Base64Image.Error.NotAnImage";
    public const string Unexpected = "L.Common.Error.Unexpected";
}