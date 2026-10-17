namespace tool_cantam.Services.Interfaces;

/// <summary>IP bilgi hata kodları; her kod yerelleştirme anahtarıdır.</summary>
public static class IpInfoErrorCodes
{
    public const string InvalidIp = "L.IpInfo.Error.InvalidIp";
    public const string NotFound = "L.IpInfo.Error.NotFound";
    public const string RateLimited = "L.IpInfo.Error.RateLimited";
    public const string NetworkError = "L.IpInfo.Error.NetworkError";
    public const string QueryTimeout = "L.IpInfo.Error.QueryTimeout";
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}