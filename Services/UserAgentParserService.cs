using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>User-Agent string parse — regex tabanlı.</summary>
public sealed class UserAgentParserService : IUserAgentParserService
{
    public const int MaxInputLengthLimit = 4096;
    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<UserAgentReport>> ParseAsync(string? ua, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ua))
            return Fail<UserAgentReport>(ToolErrorKind.Validation, UserAgentParserErrorCodes.EmptyInput, "UA is empty.");
        if (ua.Length > MaxInputLengthLimit)
            return Fail<UserAgentReport>(ToolErrorKind.TooLarge, UserAgentParserErrorCodes.InputTooLarge, "UA too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var (browser, bver) = DetectBrowser(ua);
            var (os, osver) = DetectOs(ua);
            var (device, vendor) = DetectDevice(ua);
            bool isMobile = ua.Contains("Mobile") || ua.Contains("Android") || ua.Contains("iPhone");
            bool isBot = ua.Contains("bot", StringComparison.OrdinalIgnoreCase) || ua.Contains("crawler", StringComparison.OrdinalIgnoreCase);

            sw.Stop();
            return Task.FromResult(ToolResult<UserAgentReport>.Ok(new UserAgentReport(
                browser, bver, os, osver, device, vendor, isMobile, isBot, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<UserAgentReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<UserAgentReport>(ToolErrorKind.Unexpected, UserAgentParserErrorCodes.Unexpected, ex.Message); }
    }

    private static (string, string) DetectBrowser(string ua)
    {
        if (ua.Contains("Edg/")) { var m = Regex.Match(ua, @"Edg/(\d+\.?\d*)"); return ("Microsoft Edge", m.Groups[1].Value); }
        if (ua.Contains("OPR/") || ua.Contains("Opera/")) { var m = Regex.Match(ua, @"(?:OPR|Opera)/(\d+\.?\d*)"); return ("Opera", m.Groups[1].Value); }
        if (ua.Contains("Firefox/")) { var m = Regex.Match(ua, @"Firefox/(\d+\.?\d*)"); return ("Firefox", m.Groups[1].Value); }
        if (ua.Contains("Chrome/")) { var m = Regex.Match(ua, @"Chrome/(\d+\.?\d*)"); return ("Chrome", m.Groups[1].Value); }
        if (ua.Contains("Safari/") && ua.Contains("Version/")) { var m = Regex.Match(ua, @"Version/(\d+\.?\d*)"); return ("Safari", m.Groups[1].Value); }
        return ("Unknown", "");
    }

    private static (string, string) DetectOs(string ua)
    {
        if (ua.Contains("Windows NT 10.0")) return ("Windows 10/11", "10.0");
        if (ua.Contains("Windows NT 6.3")) return ("Windows 8.1", "6.3");
        if (ua.Contains("Windows NT 6.2")) return ("Windows 8", "6.2");
        if (ua.Contains("Windows NT 6.1")) return ("Windows 7", "6.1");
        if (ua.Contains("Mac OS X")) { var m = Regex.Match(ua, @"Mac OS X (\d+)[_\.](\d+)"); return ("macOS", m.Success ? $"{m.Groups[1]}.{m.Groups[2]}" : ""); }
        if (ua.Contains("Android")) { var m = Regex.Match(ua, @"Android (\d+\.?\d*)"); return ("Android", m.Groups[1].Value); }
        if (ua.Contains("iPhone") || ua.Contains("iPad")) { var m = Regex.Match(ua, @"OS (\d+)[_\.](\d+)"); return (ua.Contains("iPad") ? "iPadOS" : "iOS", m.Success ? $"{m.Groups[1]}.{m.Groups[2]}" : ""); }
        if (ua.Contains("Linux")) return ("Linux", "");
        return ("Unknown", "");
    }

    private static (string, string) DetectDevice(string ua)
    {
        if (ua.Contains("iPhone")) return ("iPhone", "Apple");
        if (ua.Contains("iPad")) return ("iPad", "Apple");
        if (ua.Contains("Android")) return (ua.Contains("Mobile") ? "Android Phone" : "Android Tablet", "");
        if (ua.Contains("Windows")) return ("Desktop PC", "Microsoft");
        if (ua.Contains("Macintosh")) return ("Mac", "Apple");
        if (ua.Contains("Linux")) return ("Linux PC", "");
        return ("Unknown", "");
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}