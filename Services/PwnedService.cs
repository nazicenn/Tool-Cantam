using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>HaveIBeenPwned k-anonymity. Sadece SHA-1 öneki gönderilir; parola asla gönderilmez.</summary>
public sealed class PwnedService : IPwnedService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = Timeout };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ToolCantam/1.0");
        return c;
    }

    public async Task<ToolResult<PwnedReport>> CheckPasswordAsync(string? password, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(password))
            return ToolResult<PwnedReport>.Fail(ToolErrorKind.Validation, PwnedErrorCodes.EmptyPassword, "Password is empty.");

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = Convert.ToHexString(hashBytes);
            var prefix = hash[..5];
            var suffix = hash[5..];

            var response = await Client.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefix}", cts.Token);

            int count = 0;
            foreach (var line in response.Split('\n'))
            {
                if (line.StartsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var c))
                    {
                        count = c;
                        break;
                    }
                }
            }

            sw.Stop();
            return ToolResult<PwnedReport>.Ok(new PwnedReport(count, count > 0, sw.Elapsed));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<PwnedReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<PwnedReport>.Fail(ToolErrorKind.Unexpected, PwnedErrorCodes.Timeout, "Timeout.");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<PwnedReport>.Fail(ToolErrorKind.Unexpected, PwnedErrorCodes.NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<PwnedReport>.Fail(ToolErrorKind.Unexpected, PwnedErrorCodes.Unexpected, ex.Message);
        }
    }
}