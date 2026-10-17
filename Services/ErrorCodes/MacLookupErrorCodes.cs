namespace tool_cantam.Services.ErrorCodes;

/// <summary>MAC Lookup hata kodları; her kod yerelleştirme anahtarıdır.</summary>
public static class MacLookupErrorCodes
{
    public const string InvalidMac = "L.MacLookup.Error.InvalidMac";
    public const string VendorNotFound = "L.MacLookup.Error.VendorNotFound";
    public const string RateLimited = "L.MacLookup.Error.RateLimited";
    public const string NetworkError = "L.MacLookup.Error.NetworkError";
    public const string QueryTimeout = "L.MacLookup.Error.QueryTimeout";
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}