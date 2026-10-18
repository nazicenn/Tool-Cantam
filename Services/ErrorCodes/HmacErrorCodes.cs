namespace tool_cantam.Services.ErrorCodes;

public static class HmacErrorCodes
{
    public const string EmptyKey = "L.Hmac.Error.EmptyKey";
    public const string InvalidHexKey = "L.Hmac.Error.InvalidHexKey";
    public const string KeyTooLong = "L.Hmac.Error.KeyTooLong";
    public const string InputTooLarge = "L.Hmac.Error.InputTooLarge";
    public const string UnsupportedAlgorithm = "L.Hmac.Error.UnsupportedAlgorithm";
    public const string AlgorithmUnavailable = "L.Hmac.Error.AlgorithmUnavailable";
    public const string InvalidExpectedMac = "L.Hmac.Error.InvalidExpectedMac";
    public const string Unexpected = "L.Common.Error.Unexpected";
}