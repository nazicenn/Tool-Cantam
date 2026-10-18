using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Guid.NewGuid() (UUID v4) ile UUID üretir. Kültürden bağımsız upper/lower (ToUpperInvariant).
/// </summary>
public sealed class UuidGeneratorService : IUuidGeneratorService
{
    public const int MinCountLimit = 1;
    public const int MaxCountLimit = 1000;

    public int MinCount => MinCountLimit;
    public int MaxCount => MaxCountLimit;

    public Task<ToolResult<UuidReport>> GenerateAsync(UuidOptions options, CancellationToken cancellationToken)
    {
        if (options is null || options.Count < MinCountLimit || options.Count > MaxCountLimit)
            return Fail<UuidReport>(ToolErrorKind.Validation,
                UuidGeneratorErrorCodes.InvalidCount,
                $"Count must be {MinCountLimit}..{MaxCountLimit} (got {options?.Count}).");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var items = new List<string>(options.Count);
            var sb = new StringBuilder();

            for (int i = 0; i < options.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var formatted = FormatOne(Guid.NewGuid(), options);
                items.Add(formatted);
                sb.AppendLine(formatted);
            }

            sw.Stop();
            var report = new UuidReport(items, sb.ToString().TrimEnd(), sw.Elapsed, UuidVersion.V4);
            return Task.FromResult(ToolResult<UuidReport>.Ok(report));
        }
        catch (OperationCanceledException)
        {
            return Fail<UuidReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<UuidReport>(ToolErrorKind.Unexpected, UuidGeneratorErrorCodes.Unexpected, ex.Message);
        }
    }

    private static string FormatOne(Guid guid, UuidOptions o)
    {
        var s = o.IncludeHyphens ? guid.ToString("D") : guid.ToString("N");
        s = o.Case == UuidCase.Upper ? s.ToUpperInvariant() : s.ToLowerInvariant();
        if (o.IncludeBraces) s = "{" + s + "}";
        return s;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}