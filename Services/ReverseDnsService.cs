using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Reverse DNS (PTR) sorgusunun tek uygulaması.
/// </summary>
public sealed class ReverseDnsService : IReverseDnsService
{
    public const int QueryTimeoutMsLimit = 5000;

    public int QueryTimeoutMs => QueryTimeoutMsLimit;

    public ToolResult<string> ValidateIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                ReverseDnsErrorCodes.InvalidIp,
                "IP address is empty.");
        }

        string trimmed = ip.Trim();

        if (!IPAddress.TryParse(trimmed, out IPAddress? parsed))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                ReverseDnsErrorCodes.InvalidIp,
                $"'{trimmed}' is not a valid IP address.");
        }

        return ToolResult<string>.Ok(parsed.ToString());
    }

    public async Task<ToolResult<ReverseDnsReport>> LookupAsync(
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateIp(ip);
        if (!validation.IsSuccess)
            return ToolResult<ReverseDnsReport>.Fail(
                validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string address = validation.Value!;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(QueryTimeoutMsLimit);

            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(address, timeoutSource.Token)
                .ConfigureAwait(false);

            stopwatch.Stop();

            string hostName = string.IsNullOrWhiteSpace(hostEntry.HostName)
                ? address
                : hostEntry.HostName.TrimEnd('.');

            var aliases = new List<string>();
            if (hostEntry.Aliases is not null)
            {
                foreach (var alias in hostEntry.Aliases)
                {
                    string clean = alias.TrimEnd('.');
                    if (!string.IsNullOrEmpty(clean) &&
                        !string.Equals(clean, hostName, StringComparison.OrdinalIgnoreCase))
                    {
                        aliases.Add(clean);
                    }
                }
            }

            string family = IPAddress.TryParse(address, out var parsedIp)
                ? (parsedIp.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6")
                : "Unknown";

            return ToolResult<ReverseDnsReport>.Ok(new ReverseDnsReport(
                address, family, hostName, aliases, stopwatch.Elapsed));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return ToolResult<ReverseDnsReport>.Fail(
                ToolErrorKind.Unexpected,
                ReverseDnsErrorCodes.QueryTimeout,
                $"DNS query timed out after {QueryTimeoutMsLimit} ms.");
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();
            return ToolResult<ReverseDnsReport>.Fail(
                ToolErrorKind.NotFound,
                ReverseDnsErrorCodes.NotFound,
                ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<ReverseDnsReport>.Fail(
                ToolErrorKind.Unexpected,
                ReverseDnsErrorCodes.Unexpected,
                ex.ToString());
        }
    }

    private static ToolResult<ReverseDnsReport> Cancelled() =>
        ToolResult<ReverseDnsReport>.Fail(
            ToolErrorKind.Cancelled,
            ReverseDnsErrorCodes.Cancelled,
            "Reverse DNS lookup cancelled by the user.");
}