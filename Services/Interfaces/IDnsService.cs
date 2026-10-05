using System.Collections.Generic;
using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

public interface IDnsService
{
    Task<DnsRecord> LookupAsync(string domain);
    bool IsValidDomain(string domain);
}

public class DnsRecord
{
    public string Domain { get; set; } = string.Empty;
    public List<string> A { get; set; } = new();        // IPv4 adresleri
    public List<string> AAAA { get; set; } = new();     // IPv6 adresleri
    public List<string> MX { get; set; } = new();       // Mail sunucuları
    public List<string> NS { get; set; } = new();       // Name sunucuları
    public List<string> TXT { get; set; } = new();      // TXT kayıtları (SPF, DKIM)
    public List<string> CNAME { get; set; } = new();    // Canonical name
    public string SOA { get; set; } = string.Empty;     // Start of Authority
}