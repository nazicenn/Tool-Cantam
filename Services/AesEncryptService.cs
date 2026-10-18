using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// AES-256-CBC + PBKDF2 (SHA256, 100k iter). Format: salt(16) + IV(16) + ciphertext. Base64 output.
/// </summary>
public sealed class AesEncryptService : IAesEncryptService
{
    public const int MinPasswordLengthLimit = 8;
    private const int SaltSize = 16, IVSize = 16, KeySize = 32, Iterations = 100_000;

    public int MinPasswordLength => MinPasswordLengthLimit;

    public Task<ToolResult<AesReport>> EncryptAsync(string? plaintext, string? password, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(plaintext))
            return Fail<AesReport>(ToolErrorKind.Validation, AesEncryptErrorCodes.EmptyInput, "Plaintext is empty.");
        if (string.IsNullOrWhiteSpace(password))
            return Fail<AesReport>(ToolErrorKind.Validation, AesEncryptErrorCodes.EmptyPassword, "Password is empty.");
        if (password.Length < MinPasswordLengthLimit)
            return Fail<AesReport>(ToolErrorKind.Validation, AesEncryptErrorCodes.PasswordTooShort,
                $"Password must be at least {MinPasswordLengthLimit} chars.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var iv = RandomNumberGenerator.GetBytes(IVSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream();
            ms.Write(salt, 0, salt.Length);
            ms.Write(iv, 0, iv.Length);

            using (var enc = aes.CreateEncryptor())
            using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs, Encoding.UTF8))
            {
                writer.Write(plaintext);
            }

            var cipher = ms.ToArray();
            var b64 = Convert.ToBase64String(cipher);
            sw.Stop();
            return Task.FromResult(ToolResult<AesReport>.Ok(new AesReport(b64, plaintext.Length, b64.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<AesReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<AesReport>(ToolErrorKind.Unexpected, AesEncryptErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<AesReport>> DecryptAsync(string? ciphertextBase64, string? password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ciphertextBase64))
            return Fail<AesReport>(ToolErrorKind.Validation, AesEncryptErrorCodes.EmptyInput, "Ciphertext is empty.");
        if (string.IsNullOrWhiteSpace(password))
            return Fail<AesReport>(ToolErrorKind.Validation, AesEncryptErrorCodes.EmptyPassword, "Password is empty.");
        if (password.Length < MinPasswordLengthLimit)
            return Fail<AesReport>(ToolErrorKind.Validation, AesEncryptErrorCodes.PasswordTooShort,
                $"Password must be at least {MinPasswordLengthLimit} chars.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            byte[] cipher;
            try { cipher = Convert.FromBase64String(ciphertextBase64.Trim()); }
            catch (FormatException) { return Fail<AesReport>(ToolErrorKind.InvalidFormat, AesEncryptErrorCodes.InvalidBase64, "Invalid Base64."); }

            if (cipher.Length < SaltSize + IVSize + 16)
                return Fail<AesReport>(ToolErrorKind.InvalidFormat, AesEncryptErrorCodes.InvalidCipher, "Cipher too short.");

            var salt = new byte[SaltSize];
            var iv = new byte[IVSize];
            Array.Copy(cipher, 0, salt, 0, SaltSize);
            Array.Copy(cipher, SaltSize, iv, 0, IVSize);

            var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream(cipher, SaltSize + IVSize, cipher.Length - SaltSize - IVSize);
            using var dec = aes.CreateDecryptor();
            using var cs = new CryptoStream(ms, dec, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);
            var plaintext = reader.ReadToEnd();

            sw.Stop();
            return Task.FromResult(ToolResult<AesReport>.Ok(new AesReport(plaintext, ciphertextBase64.Length, plaintext.Length, sw.Elapsed)));
        }
        catch (CryptographicException ex) { return Fail<AesReport>(ToolErrorKind.InvalidFormat, AesEncryptErrorCodes.InvalidCipher, ex.Message); }
        catch (OperationCanceledException) { return Fail<AesReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<AesReport>(ToolErrorKind.Unexpected, AesEncryptErrorCodes.Unexpected, ex.Message); }
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