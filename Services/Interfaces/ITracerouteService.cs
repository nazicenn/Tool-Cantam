using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ITracerouteService
{
    int MinHops { get; }
    int MaxHopsLimit { get; }
    Task<ToolResult<TraceReport>> TraceAsync(string? host, int maxHops, IProgress<TraceHop>? progress, CancellationToken ct);
}

public sealed record TraceHop(int Ttl, string? Address, long RoundtripMs, string Status);
public sealed record TraceReport(string Host, IReadOnlyList<TraceHop> Hops, TimeSpan Elapsed, bool ReachedTarget);