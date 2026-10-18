namespace tool_cantam.Services.ErrorCodes;

public static class JwtGeneratorErrorCodes
{
    public const string EmptyKey = "L.JwtGenerator.Error.EmptyKey";
    public const string KeyTooShort = "L.JwtGenerator.Error.KeyTooShort";
    public const string KeyTooLong = "L.JwtGenerator.Error.KeyTooLong";
    public const string InvalidExpiry = "L.JwtGenerator.Error.InvalidExpiry";
    public const string InvalidClaims = "L.JwtGenerator.Error.InvalidClaims";
    public const string Unexpected = "L.Common.Error.Unexpected";
}