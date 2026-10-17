using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// JWT (JSON Web Token) decode servisinin tek uygulaması.
/// <para>
/// Header ve Payload Base64Url'den çözülür. Signature doğrulanmaz (decode-only araç).
/// Standart claim'ler (iss, sub, aud, exp, nbf, iat, jti) parse edilir.
/// </para>
/// </summary>
public sealed class JwtService : IJwtService
{
    public const int MaxTokenLengthLimit = 100 * 1024; // 100 KB

    public int MaxTokenLength => MaxTokenLengthLimit;

    public ToolResult<JwtReport> Decode(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return ToolResult<JwtReport>.Fail(
                ToolErrorKind.Validation,
                JwtErrorCodes.EmptyInput,
                "JWT token is empty.");

        string trimmed = jwt.Trim();

        if (trimmed.Length > MaxTokenLengthLimit)
            return ToolResult<JwtReport>.Fail(
                ToolErrorKind.TooLarge,
                JwtErrorCodes.TokenTooLarge,
                $"Token length {trimmed.Length} exceeds {MaxTokenLengthLimit}.");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            string[] parts = trimmed.Split('.');
            if (parts.Length < 2 || parts.Length > 3)
            {
                return ToolResult<JwtReport>.Fail(
                    ToolErrorKind.InvalidFormat,
                    JwtErrorCodes.InvalidFormat,
                    $"JWT must have 2 or 3 parts (got {parts.Length}).");
            }

            string headerJson = DecodeBase64Url(parts[0]);
            string payloadJson = DecodeBase64Url(parts[1]);
            string signature = parts.Length == 3 ? parts[2] : string.Empty;

            // JSON parse
            using var headerDoc = JsonDocument.Parse(headerJson);
            using var payloadDoc = JsonDocument.Parse(payloadJson);

            var headerRoot = headerDoc.RootElement;
            var payloadRoot = payloadDoc.RootElement;

            string algorithm = GetString(headerRoot, "alg") ?? "unknown";
            string tokenType = GetString(headerRoot, "typ") ?? "JWT";

            string? issuer = GetString(payloadRoot, "iss");
            string? subject = GetString(payloadRoot, "sub");
            string? audience = GetString(payloadRoot, "aud");
            string? jwtId = GetString(payloadRoot, "jti");

            DateTimeOffset? issuedAt = GetUnixDate(payloadRoot, "iat");
            DateTimeOffset? notBefore = GetUnixDate(payloadRoot, "nbf");
            DateTimeOffset? expiration = GetUnixDate(payloadRoot, "exp");

            var now = DateTimeOffset.UtcNow;
            bool isExpired = expiration.HasValue && now > expiration.Value;
            bool isNotYetValid = notBefore.HasValue && now < notBefore.Value;
            TimeSpan? timeUntilExpiry = expiration.HasValue ? expiration.Value - now : null;

            stopwatch.Stop();

            return ToolResult<JwtReport>.Ok(new JwtReport(
                PrettyJson(headerJson),
                PrettyJson(payloadJson),
                signature,
                algorithm,
                tokenType,
                issuer,
                subject,
                audience,
                issuedAt,
                notBefore,
                expiration,
                jwtId,
                isExpired,
                isNotYetValid,
                timeUntilExpiry,
                stopwatch.Elapsed));
        }
        catch (FormatException ex)
        {
            stopwatch.Stop();
            return ToolResult<JwtReport>.Fail(
                ToolErrorKind.InvalidFormat,
                JwtErrorCodes.InvalidBase64,
                ex.Message);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            return ToolResult<JwtReport>.Fail(
                ToolErrorKind.InvalidFormat,
                JwtErrorCodes.InvalidJson,
                $"Line {ex.LineNumber}, position {ex.BytePositionInLine}: {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<JwtReport>.Fail(
                ToolErrorKind.Unexpected,
                JwtErrorCodes.Unexpected,
                ex.Message);
        }
    }

    private static string DecodeBase64Url(string base64Url)
    {
        if (string.IsNullOrEmpty(base64Url))
            throw new FormatException("Empty Base64Url segment.");

        string padded = base64Url;
        int padLength = 4 - (base64Url.Length % 4);
        if (padLength < 4)
            padded += new string('=', padLength);

        string base64 = padded.Replace('-', '+').Replace('_', '/');
        byte[] bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static DateTimeOffset? GetUnixDate(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.Number &&
            prop.TryGetInt64(out long unixSeconds))
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static string PrettyJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}