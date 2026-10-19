using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IPwnedService
{
    Task<ToolResult<PwnedReport>> CheckPasswordAsync(string? password, CancellationToken ct);
}

public sealed record PwnedReport(int BreachCount, bool IsPwned, TimeSpan Elapsed);