using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>BCrypt.Net ile hash + verify. Work factor 4-14 arası.</summary>
public sealed class BcryptGeneratorService : IBcryptGeneratorService
{
    public const int MinWorkFactorLimit = 4;
    public const int MaxWorkFactorLimit = 14;
    public int MinWorkFactor => MinWorkFactorLimit;
    public int MaxWorkFactor => MaxWorkFactorLimit;

    public Task<ToolResult<BcryptHashReport>> HashAsync(string? password, int workFactor, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(password))
            return Fail<BcryptHashReport>(ToolErrorKind.Validation, BcryptGeneratorErrorCodes.EmptyPassword, "Password is empty.");
        if (workFactor < MinWorkFactorLimit || workFactor > MaxWorkFactorLimit)
            return Fail<BcryptHashReport>(ToolErrorKind.Validation, BcryptGeneratorErrorCodes.InvalidWork,
                $"Work factor must be {MinWorkFactorLimit}..{MaxWorkFactorLimit}.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor);
            sw.Stop();
            return Task.FromResult(ToolResult<BcryptHashReport>.Ok(new BcryptHashReport(hash, workFactor, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<BcryptHashReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<BcryptHashReport>(ToolErrorKind.Unexpected, BcryptGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<BcryptVerifyReport>> VerifyAsync(string? password, string? hash, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(password))
            return Fail<BcryptVerifyReport>(ToolErrorKind.Validation, BcryptGeneratorErrorCodes.EmptyPassword, "Password is empty.");
        if (string.IsNullOrWhiteSpace(hash))
            return Fail<BcryptVerifyReport>(ToolErrorKind.Validation, BcryptGeneratorErrorCodes.EmptyHash, "Hash is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            bool match;
            try { match = BCrypt.Net.BCrypt.Verify(password, hash); }
            catch (Exception) { return Fail<BcryptVerifyReport>(ToolErrorKind.InvalidFormat, BcryptGeneratorErrorCodes.InvalidHash, "Invalid bcrypt hash format."); }
            sw.Stop();
            return Task.FromResult(ToolResult<BcryptVerifyReport>.Ok(new BcryptVerifyReport(match, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<BcryptVerifyReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<BcryptVerifyReport>(ToolErrorKind.Unexpected, BcryptGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    public string GeneratePassword(int length = 16)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(result);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}