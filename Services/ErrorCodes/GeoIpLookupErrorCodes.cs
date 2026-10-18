namespace tool_cantam.Services.ErrorCodes;

public static class GeoIpLookupErrorCodes
{
    public const string EmptyIp = "L.GeoIpLookup.Error.EmptyIp";
    public const string InvalidIp = "L.GeoIpLookup.Error.InvalidIp";
    public const string NotFound = "L.GeoIpLookup.Error.NotFound";
    public const string RateLimited = "L.GeoIpLookup.Error.RateLimited";
    public const string NetworkError = "L.GeoIpLookup.Error.NetworkError";
    public const string Timeout = "L.GeoIpLookup.Error.Timeout";
    public const string Unexpected = "L.Common.Error.Unexpected";
}