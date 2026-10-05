using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class IpInfoService : IIpInfoService
{
    private readonly HttpClient _httpClient = new();

    public bool IsValidIp(string ip)
    {
        return System.Net.IPAddress.TryParse(ip?.Trim(), out _);
    }

    public async Task<IpInfoModel?> GetIpInfoAsync(string ip)
    {
        try
        {
            ip = ip.Trim();
            var url = $"http://ip-api.com/json/{ip}?fields=status,country,city,regionName,isp,org,timezone,query";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<IpInfoModel>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Status == "fail")
                return new IpInfoModel { Query = ip, Country = "Not found", Status = "fail" };

            return result;
        }
        catch
        {
            return new IpInfoModel { Query = ip, Country = "Error", Status = "error" };
        }
    }
}