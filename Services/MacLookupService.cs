using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class MacLookupService : IMacLookupService
{
    private readonly HttpClient _httpClient = new();

    public bool IsValidMac(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac)) return false;
        var cleanMac = mac.Trim().Replace("-", "").Replace(":", "");
        return Regex.IsMatch(cleanMac, "^[0-9A-Fa-f]{12}$");
    }

    public async Task<string> GetVendorAsync(string mac)
    {
        var cleanMac = mac.Trim().Replace("-", "").Replace(":", "").ToUpper();

        try
        {
            var url = $"https://api.maclookup.app/v2/macs/{cleanMac}";
            var response = await _httpClient.GetStringAsync(url);

            // Basit JSON parse
            var companyMatch = Regex.Match(response, "\"company\":\"([^\"]+)\"");
            if (companyMatch.Success)
                return companyMatch.Groups[1].Value;

            return "Unknown vendor";
        }
        catch
        {
            return "Error fetching vendor info";
        }
    }
}