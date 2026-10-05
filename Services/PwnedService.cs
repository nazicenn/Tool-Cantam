using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class PwnedService : IPwnedService
{
    private readonly HttpClient _httpClient = new();

    public async Task<int> CheckPasswordAsync(string password)
    {
        try
        {
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();

            var prefix = hash.Substring(0, 5);
            var suffix = hash.Substring(5);

            var url = $"https://api.pwnedpasswords.com/range/{prefix}";
            var response = await _httpClient.GetStringAsync(url);

            foreach (var line in response.Split('\n'))
            {
                if (line.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int count))
                        return count;
                }
            }
            return 0;
        }
        catch
        {
            return -1;
        }
    }
}