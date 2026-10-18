using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IIpCalculatorService
{
    Task<ToolResult<IpCalcReport>> CalculateAsync(string? ip, int cidr, CancellationToken ct);
}

public sealed record IpCalcReport(
    string IpAddress, int Cidr,
    string Network, string Broadcast,
    string SubnetMask, string WildcardMask,
    string FirstUsable, string LastUsable,
    long TotalHosts, long UsableHosts,
    string BinaryIp, string BinaryMask,
    bool IsPrivate);