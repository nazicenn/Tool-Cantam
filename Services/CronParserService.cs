using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Cron (5-parça) parser. Sonraki 5 çalışma zamanını hesaplar.</summary>
public sealed class CronParserService : ICronParserService
{
    private static readonly string[] MonthNames = { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public Task<ToolResult<CronReport>> ParseAsync(string? expr, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return Fail<CronReport>(ToolErrorKind.Validation, CronParserErrorCodes.EmptyInput, "Expression is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var parts = expr.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
                return Fail<CronReport>(ToolErrorKind.Validation, CronParserErrorCodes.InvalidParts,
                    $"Expected 5 parts (min hour dom mon dow), got {parts.Length}.");

            var fields = new List<CronField>
            {
                new("Minute",     parts[0], DescribeMinutes(parts[0]), 0, 59),
                new("Hour",       parts[1], DescribeHours(parts[1]),   0, 23),
                new("DayOfMonth", parts[2], DescribeDom(parts[2]),     1, 31),
                new("Month",      parts[3], DescribeMonth(parts[3]),   1, 12),
                new("DayOfWeek",  parts[4], DescribeDow(parts[4]),     0, 6),
            };

            var nextRuns = ComputeNextRuns(parts, 5, ct);
            var summary = BuildSummary(parts);

            sw.Stop();
            return Task.FromResult(ToolResult<CronReport>.Ok(new CronReport(expr, fields, summary, nextRuns, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<CronReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<CronReport>(ToolErrorKind.Unexpected, CronParserErrorCodes.Unexpected, ex.Message); }
    }

    private static string DescribeMinutes(string s) => s == "*" ? "every minute" : s.Contains('/') ? $"every {s.Split('/')[1]} min" : $"at minute {s}";
    private static string DescribeHours(string s) => s == "*" ? "every hour" : s.Contains('/') ? $"every {s.Split('/')[1]} h" : $"at {s}:00";
    private static string DescribeDom(string s) => s == "*" ? "every day" : s.Contains('/') ? $"every {s.Split('/')[1]} days" : $"on day {s}";
    private static string DescribeMonth(string s)
    {
        if (s == "*") return "every month";
        if (int.TryParse(s, out var m) && m >= 1 && m <= 12) return MonthNames[m];
        return s;
    }
    private static string DescribeDow(string s)
    {
        if (s == "*") return "every day";
        if (int.TryParse(s, out var d) && d >= 0 && d <= 6) return DayNames[d];
        if (s == "1-5") return "Monday-Friday (weekdays)";
        return s;
    }

    private static string BuildSummary(string[] p)
    {
        var m = p[0]; var h = p[1]; var dom = p[2]; var mon = p[3]; var dow = p[4];

        if (m == "0" && h == "0" && dom == "*" && mon == "*" && dow == "*") return "Every day at midnight (00:00)";
        if (m == "0" && h == "12" && dom == "*" && mon == "*" && dow == "*") return "Every day at noon (12:00)";
        if (m == "*/15" && h == "*") return "Every 15 minutes";
        if (m == "*/5" && h == "*") return "Every 5 minutes";
        if (m == "0" && h == "*") return "Every hour, on the hour";
        if (m == "0" && h == "9" && dow == "1-5") return "Weekdays at 09:00";
        if (m == "0" && h == "0" && dom == "1" && mon == "*") return "1st day of every month at midnight";
        return $"{h}:{m} on {dom} of {mon} (weekday {dow})";
    }

    private static List<DateTime> ComputeNextRuns(string[] parts, int count, CancellationToken ct)
    {
        var runs = new List<DateTime>(count);
        var now = DateTime.UtcNow;
        var candidate = now.AddMinutes(1);
        for (int i = 0; i < 60 * 24 * 366 && runs.Count < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (Matches(parts, candidate))
            {
                runs.Add(candidate);
                if (runs.Count >= count) break;
            }
            candidate = candidate.AddMinutes(1);
        }
        return runs;
    }

    private static bool Matches(string[] parts, DateTime dt)
        => MatchField(parts[0], dt.Minute, 0, 59)
        && MatchField(parts[1], dt.Hour, 0, 23)
        && MatchField(parts[2], dt.Day, 1, 31)
        && MatchField(parts[3], dt.Month, 1, 12)
        && MatchField(parts[4], (int)dt.DayOfWeek, 0, 6);

    private static bool MatchField(string field, int value, int min, int max)
    {
        if (field == "*") return true;
        foreach (var part in field.Split(','))
        {
            if (part.Contains('/'))
            {
                var seg = part.Split('/');
                var range = seg[0];
                if (!int.TryParse(seg[1], out var step) || step <= 0) continue;
                int lo = min, hi = max;
                if (range != "*" && range.Contains('-'))
                {
                    var r = range.Split('-');
                    if (int.TryParse(r[0], out lo) && int.TryParse(r[1], out hi))
                    {
                        if (value >= lo && value <= hi && (value - lo) % step == 0) return true;
                    }
                }
                else if ((value - min) % step == 0) return true;
                continue;
            }
            if (part.Contains('-'))
            {
                var r = part.Split('-');
                if (int.TryParse(r[0], out var lo) && int.TryParse(r[1], out var hi))
                {
                    if (value >= lo && value <= hi) return true;
                }
                continue;
            }
            if (int.TryParse(part, out var v) && v == value) return true;
        }
        return false;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}