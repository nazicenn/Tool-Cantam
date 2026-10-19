using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IUserAgentParserService
{
    int MaxInputLength { get; }
    Task<ToolResult<UserAgentReport>> ParseAsync(string? ua, CancellationToken ct);
}

public sealed record UserAgentReport(
    string Browser, string BrowserVersion,
    string OperatingSystem, string OsVersion,
    string DeviceType, string DeviceVendor,
    bool IsMobile, bool IsBot,
    TimeSpan Elapsed);