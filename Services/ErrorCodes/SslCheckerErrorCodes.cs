namespace tool_cantam.Services.ErrorCodes;

/// <summary>SSL Checker hata kodları; her kod yerelleştirme anahtarıdır.</summary>
public static class SslCheckerErrorCodes
{
    public const string InvalidDomain = "L.SslChecker.Error.InvalidDomain";
    public const string ConnectionFailed = "L.SslChecker.Error.ConnectionFailed";
    public const string Timeout = "L.SslChecker.Error.Timeout";
    public const string NoCertificate = "L.SslChecker.Error.NoCertificate";
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}