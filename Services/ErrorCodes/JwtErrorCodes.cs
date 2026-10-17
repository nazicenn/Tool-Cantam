namespace tool_cantam.Services.ErrorCodes;

public static class JwtErrorCodes
{
    public const string EmptyInput = "L.Jwt.Error.EmptyInput";
    public const string TokenTooLarge = "L.Jwt.Error.TokenTooLarge";
    public const string InvalidFormat = "L.Jwt.Error.InvalidFormat";
    public const string InvalidBase64 = "L.Jwt.Error.InvalidBase64";
    public const string InvalidJson = "L.Jwt.Error.InvalidJson";
    public const string Unexpected = "L.Common.Error.Unexpected";
}