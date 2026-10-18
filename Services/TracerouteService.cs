using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>ICMP TTL artırarak traceroute. Yönetici izni gerekebilir.</summary>
public sealed class TracerouteService : ITracerouteService
{
    public const int MinHopsLimit = 1;
    public const int MaxHopsLimitValue = 64;
    public int MinHops => MinHopsLimit;
    public int MaxHopsLimit => MaxHopsLimitValue;

    public async Task<ToolResult<TraceReport>> TraceAsync(
        string? host, int maxHops, IProgress<TraceHop>? progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host))
            return ToolResult<TraceReport>.Fail(ToolErrorKind.Validation, TracerouteErrorCodes.EmptyHost, "Host is empty.");
        if (maxHops < MinHopsLimit || maxHops > MaxHopsLimitValue)
            return ToolResult<TraceReport>.Fail(ToolErrorKind.Validation, TracerouteErrorCodes.InvalidHops,
                $"Hops must be {MinHopsLimit}..{MaxHopsLimitValue}.");

        try
        {
            IPAddress? target = null;
            try { target = (await Dns.GetHostAddressesAsync(host)).FirstOrDefault(); } catch { }
            if (target is null)
                return ToolResult<TraceReport>.Fail(ToolErrorKind.NotFound, TracerouteErrorCodes.InvalidHost, "Cannot resolve host.");

            var sw = Stopwatch.StartNew();
            var hops = new List<TraceHop>();
            bool reached = false;

            await Task.Run(() =>
            {
                using var ping = new Ping();
                for (int ttl = 1; ttl <= maxHops; ttl++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var options = new PingOptions(ttl, true);
                        var reply = ping.Send(target, 5000, new byte[32], options);
                        if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
                        {
                            var addr = reply.Address?.ToString();
                            var hop = new TraceHop(ttl, addr, reply.RoundtripTime, reply.Status.ToString());
                            hops.Add(hop);
                            progress?.Report(hop);
                            if (reply.Status == IPStatus.Success && reply.Address?.Equals(target) == true) { reached = true; break; }
                        }
                        else
                        {
                            var hop = new TraceHop(ttl, "*", 0, reply.Status.ToString());
                            hops.Add(hop);
                            progress?.Report(hop);
                        }
                    }
                    catch (Exception ex)
                    {
                        var hop = new TraceHop(ttl, "*", 0, "Error: " + ex.Message);
                        hops.Add(hop);
                        progress?.Report(hop);
                    }
                }
            }, ct);

            sw.Stop();
            return ToolResult<TraceReport>.Ok(new TraceReport(host, hops, sw.Elapsed, reached));
        }
        catch (OperationCanceledException) { return ToolResult<TraceReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return ToolResult<TraceReport>.Fail(ToolErrorKind.Unexpected, TracerouteErrorCodes.Unexpected, ex.Message); }
    }
}