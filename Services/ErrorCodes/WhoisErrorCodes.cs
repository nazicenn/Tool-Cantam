namespace tool_cantam.Services.ErrorCodes;

/// <summary>WHOIS hata kodları; her kod yerelleştirme anahtarıdır.</summary>
public static class WhoisErrorCodes
{
    public const string InvalidDomain = "L.Whois.Error.InvalidDomain";
    public const string UnsupportedTld = "L.Whois.Error.UnsupportedTld";
    public const string ServerUnreachable = "L.Whois.Error.ServerUnreachable";
    public const string NoData = "L.Whois.Error.NoData";
    public const string QueryTimeout = "L.Whois.Error.QueryTimeout";
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}