using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>IPv4 subnet hesaplayıcı. CIDR 0-32.</summary>
public sealed class IpCalculatorService : IIpCalculatorService
{
    public Task<ToolResult<IpCalcReport>> CalculateAsync(string? ip, int cidr, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return Fail<IpCalcReport>(ToolErrorKind.Validation, IpCalculatorErrorCodes.EmptyIp, "IP is empty.");
        if (cidr < 0 || cidr > 32)
            return Fail<IpCalcReport>(ToolErrorKind.Validation, IpCalculatorErrorCodes.InvalidCidr, $"CIDR must be 0..32 (got {cidr}).");
        if (!IPAddress.TryParse(ip, out var addr))
            return Fail<IpCalcReport>(ToolErrorKind.Validation, IpCalculatorErrorCodes.InvalidIp, $"Invalid IP: {ip}");
        if (addr.AddressFamily != AddressFamily.InterNetwork)
            return Fail<IpCalcReport>(ToolErrorKind.Validation, IpCalculatorErrorCodes.Ipv6Unsupported, "IPv6 not supported.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var ipBytes = addr.GetAddressBytes();
            uint ipNum = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) | ((uint)ipBytes[2] << 8) | ipBytes[3];
            uint mask = cidr == 0 ? 0u : 0xFFFFFFFFu << (32 - cidr);
            uint network = ipNum & mask;
            uint broadcast = network | ~mask;

            uint totalHosts = cidr == 32 ? 1 : (uint)1 << (32 - cidr);
            uint usable = totalHosts > 2 ? totalHosts - 2 : 0;
            uint first = usable > 0 ? network + 1 : network;
            uint last = usable > 0 ? broadcast - 1 : broadcast;

            var report = new IpCalcReport(
                addr.ToString(), cidr,
                NumToIp(network), NumToIp(broadcast),
                NumToIp(mask), NumToIp(~mask),
                NumToIp(first), NumToIp(last),
                totalHosts, usable,
                BinStr(ipBytes), BinStr(FromUInt(mask)),
                IsPrivate(ipBytes));

            return Task.FromResult(ToolResult<IpCalcReport>.Ok(report));
        }
        catch (OperationCanceledException) { return Fail<IpCalcReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<IpCalcReport>(ToolErrorKind.Unexpected, IpCalculatorErrorCodes.Unexpected, ex.Message); }
    }

    private static string NumToIp(uint n)
    {
        var b = FromUInt(n);
        return new IPAddress(b).ToString();
    }

    private static byte[] FromUInt(uint n)
        => new[] { (byte)(n >> 24), (byte)(n >> 16), (byte)(n >> 8), (byte)n };

    private static string BinStr(byte[] b) => string.Join(".", Array.ConvertAll(b, x => Convert.ToString(x, 2).PadLeft(8, '0')));

    private static bool IsPrivate(byte[] ip)
    {
        // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
        if (ip[0] == 10) return true;
        if (ip[0] == 172 && ip[1] >= 16 && ip[1] <= 31) return true;
        if (ip[0] == 192 && ip[1] == 168) return true;
        return false;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}