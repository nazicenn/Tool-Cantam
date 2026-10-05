using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class WhoisService : IWhoisService
{
    public bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return false;
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }

    public async Task<WhoisModel> LookupAsync(string domain)
    {
        var result = new WhoisModel { Domain = domain };

        // Select WHOIS server based on domain extension
        string whoisServer = GetWhoisServer(domain);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(whoisServer, 43);

            using var stream = client.GetStream();
            var request = Encoding.ASCII.GetBytes($"{domain}\r\n");
            await stream.WriteAsync(request, 0, request.Length);

            var response = new StringBuilder();
            var buffer = new byte[8192];
            int bytesRead;

            do
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }
            while (bytesRead > 0);

            var rawData = response.ToString();
            result.RawData = rawData;
            result.Success = true;

            ParseWhoisResponse(result, rawData);
        }
        catch (SocketException ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message switch
            {
                var msg when msg.Contains("No such host") => "WHOIS server not reachable",
                var msg when msg.Contains("Connection refused") => "Connection refused by WHOIS server",
                _ => $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"WHOIS lookup failed: {ex.Message}";
        }

        return result;
    }

    private string GetWhoisServer(string domain)
    {
        var tld = domain.Substring(domain.LastIndexOf('.') + 1).ToLower();

        return tld switch
        {
            "com" => "whois.verisign-grs.com",
            "org" => "whois.pir.org",
            "net" => "whois.verisign-grs.com",
            "edu" => "whois.educause.edu",
            "gov" => "whois.nic.gov",
            "tr" => "whois.nic.tr",
            "uk" => "whois.nic.uk",
            "de" => "whois.denic.de",
            "fr" => "whois.nic.fr",
            "jp" => "whois.jprs.jp",
            "ru" => "whois.tcinet.ru",
            "cn" => "whois.cnnic.cn",
            "io" => "whois.nic.io",
            "app" => "whois.nic.google",
            "dev" => "whois.nic.google",
            _ => "whois.verisign-grs.com"
        };
    }

    private void ParseWhoisResponse(WhoisModel model, string rawData)
    {
        var lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            // Registrar
            if (line.Contains("Registrar:", StringComparison.OrdinalIgnoreCase))
                model.Registrar = ExtractValue(line);

            // Creation Date
            else if (line.Contains("Creation Date:", StringComparison.OrdinalIgnoreCase))
                model.CreationDate = ExtractValue(line);

            // Expiration Date
            else if (line.Contains("Registry Expiry Date:", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("Expiration Date:", StringComparison.OrdinalIgnoreCase))
                model.ExpirationDate = ExtractValue(line);

            // Updated Date
            else if (line.Contains("Updated Date:", StringComparison.OrdinalIgnoreCase))
                model.UpdatedDate = ExtractValue(line);

            // Name Servers
            else if (line.Contains("Name Server:", StringComparison.OrdinalIgnoreCase))
            {
                var ns = ExtractValue(line);
                if (!string.IsNullOrEmpty(ns) && !model.NameServers.Contains(ns))
                    model.NameServers += (string.IsNullOrEmpty(model.NameServers) ? "" : ", ") + ns;
            }

            // Domain Status
            else if (line.Contains("Domain Status:", StringComparison.OrdinalIgnoreCase))
            {
                var status = ExtractValue(line);
                if (!string.IsNullOrEmpty(status) && !model.Status.Contains(status))
                    model.Status += (string.IsNullOrEmpty(model.Status) ? "" : ", ") + status;
            }

            // Registrant (Owner)
            else if (line.Contains("Registrant Name:", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("Registrant Organization:", StringComparison.OrdinalIgnoreCase))
            {
                var owner = ExtractValue(line);
                if (!string.IsNullOrEmpty(owner))
                    model.Owner = owner;
            }

            // Registrant Country
            else if (line.Contains("Registrant Country:", StringComparison.OrdinalIgnoreCase))
                model.OwnerCountry = ExtractValue(line);
        }

        // Cleanup
        model.NameServers = model.NameServers.Trim(',', ' ').Trim();
        model.Status = model.Status.Trim(',', ' ').Trim();

        // If no data found
        if (string.IsNullOrEmpty(model.Registrar) &&
            string.IsNullOrEmpty(model.CreationDate) &&
            string.IsNullOrEmpty(model.NameServers))
        {
            model.Registrar = "Information not available";
        }
    }

    private string ExtractValue(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex > 0 && colonIndex + 1 < line.Length)
            return line.Substring(colonIndex + 1).Trim();
        return string.Empty;
    }
}