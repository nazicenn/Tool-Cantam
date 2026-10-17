using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// ICMP ping sorgusunun tek uygulaması. Hedefi bir kez çözer, ardından sıralı denemeler yapar;
/// her deneme kendi zaman aşımına sahiptir ve kullanıcı iptali alt katmana iletilir.
/// </summary>
public sealed class PingService : IPingService
{
    public const int MaxCountLimit = 20;
    public const int MinTimeoutLimit = 100;
    public const int MaxTimeoutLimit = 10000;

    public int MaxCount => MaxCountLimit;
    public int MinTimeoutMs => MinTimeoutLimit;
    public int MaxTimeoutMs => MaxTimeoutLimit;

    public ToolResult<string> ValidateTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation, PingErrorCodes.InvalidTarget, "Target is empty.");

        string trimmed = target.Trim();

        if (trimmed.Length > 253)
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation, PingErrorCodes.InvalidTarget,
                $"Target is too long ({trimmed.Length}).");

        if (trimmed.Contains(' ') || trimmed.Contains('/') || trimmed.Contains(':'))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation, PingErrorCodes.InvalidTarget,
                $"Target contains invalid characters: '{trimmed}'.");

        // ★ Alt çizgi (underscore) reddet
        if (trimmed.Contains('_'))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation, PingErrorCodes.InvalidTarget,
                $"Target contains underscore: '{trimmed}'.");

        if (IPAddress.TryParse(trimmed, out _))
            return ToolResult<string>.Ok(trimmed);

        if (Uri.CheckHostName(trimmed) == UriHostNameType.Dns)
            return ToolResult<string>.Ok(trimmed);

        return ToolResult<string>.Fail(
            ToolErrorKind.Validation, PingErrorCodes.InvalidTarget,
            $"'{trimmed}' is not a valid host or IP address.");
    }

    public async Task<ToolResult<PingReport>> PingAsync(
        string? target,
        int count,
        int timeoutMs,
        IProgress<PingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateTarget(target);
        if (!validation.IsSuccess)
            return ToolResult<PingReport>.Fail(
                validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        if (count < 1 || count > MaxCountLimit)
            return ToolResult<PingReport>.Fail(
                ToolErrorKind.Validation, PingErrorCodes.InvalidCount,
                $"Count must be 1..{MaxCountLimit} (got {count}).");

        if (timeoutMs < MinTimeoutLimit || timeoutMs > MaxTimeoutLimit)
            return ToolResult<PingReport>.Fail(
                ToolErrorKind.Validation, PingErrorCodes.InvalidTimeout,
                $"Timeout must be {MinTimeoutLimit}..{MaxTimeoutLimit} ms (got {timeoutMs}).");

        string host = validation.Value!;

        // Hedefi bir kez çöz
        IPAddress[] resolved;
        try
        {
            resolved = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return Cancelled(); }
        catch (SocketException ex)
        {
            return ToolResult<PingReport>.Fail(
                ToolErrorKind.NotFound, PingErrorCodes.TargetNotFound, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<PingReport>.Fail(
                ToolErrorKind.Unexpected, PingErrorCodes.Unexpected, ex.Message);
        }

        var address = resolved.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                      ?? resolved.FirstOrDefault();

        if (address is null)
            return ToolResult<PingReport>.Fail(
                ToolErrorKind.NotFound, PingErrorCodes.TargetNotFound,
                $"No address found for '{host}'.");

        var attempts = new List<PingAttempt>(count);
        var stopwatch = Stopwatch.StartNew();

        int sent = 0, received = 0, lost = 0;
        long totalMs = 0;
        long minMs = long.MaxValue;
        long maxMs = 0;

        using var ping = new Ping();

        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                return Cancelled();
            }

            sent++;

            try
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutSource.CancelAfter(timeoutMs + 1000);

                PingReply reply = await ping.SendPingAsync(address, timeoutMs).ConfigureAwait(false);

                if (reply.Status == IPStatus.Success)
                {
                    long ms = reply.RoundtripTime;
                    received++;
                    totalMs += ms;
                    if (ms < minMs) minMs = ms;
                    if (ms > maxMs) maxMs = ms;

                    attempts.Add(new PingAttempt(i + 1, reply.Status.ToString(), ms, reply.Address?.ToString() ?? ""));
                }
                else
                {
                    lost++;
                    attempts.Add(new PingAttempt(i + 1, reply.Status.ToString(), null, reply.Address?.ToString() ?? ""));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                return Cancelled();
            }
            catch (PingException)
            {
                lost++;
                attempts.Add(new PingAttempt(i + 1, "ICMP Blocked", null, ""));
            }
            catch (Exception ex)
            {
                lost++;
                attempts.Add(new PingAttempt(i + 1, $"Error: {ex.Message}", null, ""));
            }

            progress?.Report(new PingProgress(i + 1, count, received));
        }

        stopwatch.Stop();

        int packetLoss = sent > 0 ? (lost * 100 / sent) : 0;
        long? avg = received > 0 ? (totalMs / received) : null;
        long? min = received > 0 ? minMs : null;
        long? max = received > 0 ? maxMs : null;

        return ToolResult<PingReport>.Ok(new PingReport(
            host,
            address.ToString(),
            sent, received, lost, packetLoss,
            min, max, avg,
            attempts,
            stopwatch.Elapsed));
    }

    private static ToolResult<PingReport> Cancelled() =>
        ToolResult<PingReport>.Fail(
            ToolErrorKind.Cancelled, PingErrorCodes.Cancelled,
            "Ping cancelled by the user.");
}