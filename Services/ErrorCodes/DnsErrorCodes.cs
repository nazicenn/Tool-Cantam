namespace tool_cantam.Services.ErrorCodes;

/// <summary>DNS sorgusunun hata kodları; her kod yerelleştirme anahtarıdır.</summary>
public static class DnsErrorCodes
{
    public const string InvalidDomain = "L.Dns.Error.InvalidDomain";
    public const string DomainNotFound = "L.Dns.Error.DomainNotFound";
    public const string QueryTimeout = "L.Dns.Error.QueryTimeout";
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}