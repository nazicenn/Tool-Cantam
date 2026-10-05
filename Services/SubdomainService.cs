using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class SubdomainService : ISubdomainService
{
    // Common subdomain listesi (en çok kullanılanlar)
    private readonly List<string> _commonSubdomains = new()
    {
        "www", "mail", "ftp", "localhost", "webmail", "smtp", "pop", "ns1", "webdisk",
        "ns2", "cpanel", "whm", "autodiscover", "autoconfig", "m", "imap", "test", "ns",
        "blog", "pop3", "dev", "www2", "admin", "forum", "news", "vpn", "ns3", "mail2",
        "new", "mysql", "old", "lists", "support", "mobile", "mx", "static", "docs",
        "beta", "shop", "sql", "secure", "demo", "cp", "calendar", "wiki", "web",
        "media", "email", "images", "img", "download", "dns", "piwik", "stats",
        "dashboard", "portal", "manage", "start", "info", "app", "store", "shop",
        "api", "gateway", "oauth", "login", "auth", "sso", "id", "account"
    };

    public bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return false;

        // Basit domain kontrolü
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }

    public async Task<List<string>> ScanAsync(string domain)
    {
        return await Task.Run(() =>
        {
            var found = new List<string>();

            foreach (var sub in _commonSubdomains)
            {
                var fullDomain = $"{sub}.{domain}";

                try
                {
                    // DNS sorgusu ile subdomain'in var olup olmadığını kontrol et
                    var hostEntry = Dns.GetHostEntry(fullDomain);
                    if (hostEntry.AddressList.Length > 0)
                    {
                        found.Add(fullDomain);
                    }
                }
                catch
                {
                    // Subdomain yok, devam et
                }
            }

            return found.OrderBy(x => x).ToList();
        });
    }
}