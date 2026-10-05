using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Base64 kodlama/çözme servis uygulaması.
/// Hata durumunda exception fırlatır; çağıran ViewModel katmanı yakalar ve kullanıcıya bildirir.
/// </summary>
public class Base64Service : IBase64Service
{
    /// <summary>Maksimum metin girişi boyutu (10 MB). Bellek taşmasını önlemek için sınır.</summary>
    private const int MaxTextLengthBytes = 10 * 1024 * 1024;

    public string EncodeText(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        byte[] bytes = Encoding.UTF8.GetBytes(plainText);
        if (bytes.Length > MaxTextLengthBytes)
            throw new ArgumentException($"Input text is too large ({bytes.Length / 1024 / 1024} MB). Maximum: {MaxTextLengthBytes / 1024 / 1024} MB.");

        return Convert.ToBase64String(bytes);
    }

    public string DecodeText(string base64Text)
    {
        if (string.IsNullOrWhiteSpace(base64Text)) return string.Empty;

        string sanitized = base64Text.Trim().Replace(" ", "+");
        int mod4 = sanitized.Length % 4;
        if (mod4 > 0) sanitized += new string('=', 4 - mod4);

        byte[] decoded = Convert.FromBase64String(sanitized);
        return Encoding.UTF8.GetString(decoded);
    }

    public async Task<bool> EncodeFileAsync(string filePath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Source file not found.", filePath);

        using var fileIn = File.OpenRead(filePath);
        using var fileOut = File.Create(outputPath);
        using var crypto = new System.Security.Cryptography.CryptoStream(
            fileOut,
            new System.Security.Cryptography.ToBase64Transform(),
            System.Security.Cryptography.CryptoStreamMode.Write);

        await fileIn.CopyToAsync(crypto, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DecodeFileAsync(string filePath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Source file not found.", filePath);

        using var fileIn = File.OpenRead(filePath);
        using var fileOut = File.Create(outputPath);
        using var crypto = new System.Security.Cryptography.CryptoStream(
            fileIn,
            new System.Security.Cryptography.FromBase64Transform(),
            System.Security.Cryptography.CryptoStreamMode.Read);

        await crypto.CopyToAsync(fileOut, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
