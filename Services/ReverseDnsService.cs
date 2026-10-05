using System;
using System.Net;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class ReverseDnsService : IReverseDnsService
{
    public bool IsValidIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        return IPAddress.TryParse(ip.Trim(), out _);
    }

    public async Task<string> LookupAsync(string ip)
    {
        return await Task.Run(() =>
        {
            try
            {
                var cleanIp = ip.Trim();
                var hostEntry = Dns.GetHostEntry(cleanIp);
                return hostEntry.HostName;
            }
            catch
            {
                return "No PTR record found";
            }
        });
    }
}