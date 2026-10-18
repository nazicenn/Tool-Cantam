using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Microsoft.IdentityModel.Tokens ile HS256/384/512 JWT üretir. Secret key min 32 char.
/// </summary>
public sealed class JwtGeneratorService : IJwtGeneratorService
{
    public const int MinKeyLengthLimit = 32;
    public const int MaxKeyLengthLimit = 4096;
    public int MinKeyLength => MinKeyLengthLimit;
    public int MaxKeyLength => MaxKeyLengthLimit;

    public Task<ToolResult<JwtGenReport>> GenerateAsync(JwtOptions o, CancellationToken ct)
    {
        if (o is null || string.IsNullOrWhiteSpace(o.SecretKey))
            return Fail<JwtGenReport>(ToolErrorKind.Validation, JwtGeneratorErrorCodes.EmptyKey, "Secret key is empty.");
        if (o.SecretKey.Length < MinKeyLengthLimit)
            return Fail<JwtGenReport>(ToolErrorKind.Validation, JwtGeneratorErrorCodes.KeyTooShort,
                $"Key must be at least {MinKeyLengthLimit} chars (got {o.SecretKey.Length}).");
        if (o.SecretKey.Length > MaxKeyLengthLimit)
            return Fail<JwtGenReport>(ToolErrorKind.Validation, JwtGeneratorErrorCodes.KeyTooLong,
                $"Key must be at most {MaxKeyLengthLimit} chars.");
        if (o.ExpiryMinutes < 1 || o.ExpiryMinutes > 1440 * 30)
            return Fail<JwtGenReport>(ToolErrorKind.Validation, JwtGeneratorErrorCodes.InvalidExpiry,
                "Expiry must be 1..43200 minutes.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var claims = ParseClaims(o.Claims);
            if (claims is null)
                return Fail<JwtGenReport>(ToolErrorKind.Validation, JwtGeneratorErrorCodes.InvalidClaims,
                    "Claims must be key:value per line.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SecretKey));
            var creds = new SigningCredentials(key, MapAlgo(o.Algorithm));
            var expires = DateTime.UtcNow.AddMinutes(o.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: string.IsNullOrWhiteSpace(o.Issuer) ? null : o.Issuer,
                audience: string.IsNullOrWhiteSpace(o.Audience) ? null : o.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds);

            var handler = new JwtSecurityTokenHandler();
            var tokenStr = handler.WriteToken(token);
            sw.Stop();

            var parts = tokenStr.Split('.');
            return Task.FromResult(ToolResult<JwtGenReport>.Ok(new JwtGenReport(
                tokenStr, o.Algorithm.ToString(), expires, claims.Count,
                parts[0].Length, parts[1].Length, parts[2].Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<JwtGenReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<JwtGenReport>(ToolErrorKind.Unexpected, JwtGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    private static List<Claim>? ParseClaims(string input)
    {
        var claims = new List<Claim>();
        if (string.IsNullOrWhiteSpace(input)) return claims;

        foreach (var raw in input.Split('\n'))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var idx = line.IndexOf(':');
            if (idx <= 0 || idx >= line.Length - 1) return null;
            claims.Add(new Claim(line[..idx].Trim(), line[(idx + 1)..].Trim()));
        }
        return claims;
    }

    private static string MapAlgo(JwtAlgorithm a) => a switch
    {
        JwtAlgorithm.HS384 => SecurityAlgorithms.HmacSha384,
        JwtAlgorithm.HS512 => SecurityAlgorithms.HmacSha512,
        _ => SecurityAlgorithms.HmacSha256,
    };

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}