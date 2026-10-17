namespace tool_cantam.Services.ErrorCodes;

public static class JsonFormatterErrorCodes
{
    public const string InputTooLarge = "L.JsonFormatter.Error.InputTooLarge";
    public const string EmptyInput = "L.JsonFormatter.Error.EmptyInput";
    public const string InvalidJson = "L.JsonFormatter.Error.InvalidJson";
    public const string DepthTooLarge = "L.JsonFormatter.Error.DepthTooLarge";
    public const string InvalidIndent = "L.JsonFormatter.Error.InvalidIndent";
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}