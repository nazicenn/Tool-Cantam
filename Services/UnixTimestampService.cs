using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Unix timestamp ve DateTime arasında kültürden bağımsız dönüşüm.
/// Geçersiz girdiler <see cref="ToolResult{T}"/> ile sınıflandırılmış hata olarak döner.
/// </summary>
public sealed class UnixTimestampService : IUnixTimestampService
{
    public long MinSeconds => -62135596800L;  // 0001-01-01 00:00:00 UTC
    public long MaxSeconds => 253402300799L;  // 9999-12-31 23:59:59 UTC

    private static readonly string[] AcceptedFormats =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "O",
        "R",
    };

    public Task<ToolResult<UnixTimestampReport>> FromUnixAsync(
        string? input,
        TimestampUnit unit,
        TimeDisplayMode mode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<UnixTimestampReport>(ToolErrorKind.Validation,
                UnixTimestampErrorCodes.EmptyInput, "Timestamp input is empty.");

        if (!long.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return Fail<UnixTimestampReport>(ToolErrorKind.Validation,
                UnixTimestampErrorCodes.InvalidNumber, $"Invalid number: '{input}'.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            long seconds = unit == TimestampUnit.Seconds ? value : value / 1000L;
            if (seconds < MinSeconds || seconds > MaxSeconds)
                return Fail<UnixTimestampReport>(ToolErrorKind.Validation,
                    UnixTimestampErrorCodes.OutOfRange,
                    $"Value {value} is outside supported range ({MinSeconds}..{MaxSeconds}).");

            var dto = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return Task.FromResult(ToolResult<UnixTimestampReport>.Ok(Build(dto, mode)));
        }
        catch (OperationCanceledException)
        {
            return Fail<UnixTimestampReport>(ToolErrorKind.Cancelled,
                "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<UnixTimestampReport>(ToolErrorKind.Unexpected,
                UnixTimestampErrorCodes.Unexpected, ex.Message);
        }
    }

    public Task<ToolResult<UnixTimestampReport>> ToUnixAsync(
        string? input,
        TimeDisplayMode mode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<UnixTimestampReport>(ToolErrorKind.Validation,
                UnixTimestampErrorCodes.EmptyInput, "Date input is empty.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmed = input.Trim();
            if (!DateTimeOffset.TryParseExact(
                    trimmed,
                    AcceptedFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dto) &&
                !DateTimeOffset.TryParse(
                    trimmed,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out dto))
            {
                return Fail<UnixTimestampReport>(ToolErrorKind.Validation,
                    UnixTimestampErrorCodes.InvalidDate,
                    $"Unrecognized date format: '{input}'.");
            }

            return Task.FromResult(ToolResult<UnixTimestampReport>.Ok(Build(dto, mode)));
        }
        catch (OperationCanceledException)
        {
            return Fail<UnixTimestampReport>(ToolErrorKind.Cancelled,
                "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<UnixTimestampReport>(ToolErrorKind.Unexpected,
                UnixTimestampErrorCodes.Unexpected, ex.Message);
        }
    }

    public UnixTimestampReport Now()
        => Build(DateTimeOffset.UtcNow, TimeDisplayMode.Utc);

    private static UnixTimestampReport Build(DateTimeOffset dto, TimeDisplayMode mode)
    {
        var utc = dto.UtcDateTime;
        var local = dto.LocalDateTime;

        var display = mode == TimeDisplayMode.Utc ? utc : local;
        var suffix = mode == TimeDisplayMode.Utc ? " UTC" : string.Empty;

        var readable = display.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + suffix;
        var iso = utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var rfc = utc.ToString("R", CultureInfo.InvariantCulture);

        return new UnixTimestampReport(
            dto.ToUnixTimeSeconds(),
            dto.ToUnixTimeMilliseconds(),
            utc,
            local,
            iso,
            rfc,
            readable,
            mode);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}