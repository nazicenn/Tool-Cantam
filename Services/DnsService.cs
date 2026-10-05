using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class DnsService : IDnsService
{
    private readonly LookupClient _dnsClient = new();

    public bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return false;
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }

    public async Task<DnsRecord> LookupAsync(string domain)
    {
        var record = new DnsRecord { Domain = domain };

        // A / AAAA Records
        try
        {
            var hostEntry = await _dnsClient.GetHostEntryAsync(domain);
            if (hostEntry != null)
            {
                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        record.A.Add(ip.ToString());
                    else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        record.AAAA.Add(ip.ToString());
                }
            }
        }
        catch { }

        // MX Records
        try
        {
            foreach (var mx in await GetMXRecordsAsync(domain))
                record.MX.Add(mx);
        }
        catch { }

        // NS Records
        try
        {
            foreach (var ns in await GetNSRecordsAsync(domain))
                record.NS.Add(ns);
        }
        catch { }

        // TXT Records
        try
        {
            foreach (var txt in await GetTxtRecordsAsync(domain))
                record.TXT.Add(txt);
        }
        catch { }

        return record;
    }

    // Kayıtlar nslookup biçiminde döner: "<priority> <exchange>"
    private async Task<List<string>> GetMXRecordsAsync(string domain)
    {
        var results = new List<string>();

        var response = await _dnsClient.QueryAsync(domain, QueryType.MX, QueryClass.IN);
        if (response.HasError) return results;

        results.AddRange(response.Answers.MxRecords()
            .Select(mx => (Priority: mx.Preference, Exchange: mx.Exchange.Value.TrimEnd('.')))
            .OrderBy(mx => mx.Priority)
            .Select(mx => $"{mx.Priority} {mx.Exchange}"));

        return results;
    }

    private async Task<List<string>> GetNSRecordsAsync(string domain)
    {
        var results = new List<string>();

        var response = await _dnsClient.QueryAsync(domain, QueryType.NS, QueryClass.IN);
        if (response.HasError) return results;

        results.AddRange(response.Answers.NsRecords()
            .Select(ns => ns.NSDName.Value.TrimEnd('.'))
            .Where(name => name.Length > 0)
            .Distinct());

        return results;
    }

    private async Task<List<string>> GetTxtRecordsAsync(string domain)
    {
        var results = new List<string>();

        var response = await _dnsClient.QueryAsync(domain, QueryType.TXT, QueryClass.IN);
        if (response.HasError) return results;

        results.AddRange(response.Answers.TxtRecords()
            .Select(txt => string.Join("", txt.EscapedText))
            .Where(text => text.Length > 0));

        return results;
    }
}