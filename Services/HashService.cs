using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Kriptografik hash hesaplama servis uygulaması.
/// Desteklenen algoritmalar: MD5, SHA1, SHA256, SHA384, SHA512.
/// </summary>
public class HashService : IHashService
{
    private static readonly string[] SupportedAlgorithms = ["MD5", "SHA1", "SHA256", "SHA384", "SHA512"];

    private static void ValidateAlgorithm(string algorithm)
    {
        if (!Array.Exists(SupportedAlgorithms, a => a == algorithm))
            throw new ArgumentException($"Unsupported algorithm: '{algorithm}'. Valid values: {string.Join(", ", SupportedAlgorithms)}");
    }

    public string ComputeFromText(string input, string algorithm)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        ValidateAlgorithm(algorithm);

        byte[] bytes = Array.Empty<byte>();
        try
        {
            bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = CryptographicOperations.HashData(new HashAlgorithmName(algorithm), bytes);
            return Convert.ToHexString(hash);
        }
        finally
        {
            // Hassas veri güvenliği: Bellek temizliği
            if (bytes.Length > 0)
                CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public async Task<string> ComputeFromFileAsync(string filePath, string algorithm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);
        ValidateAlgorithm(algorithm);

        using var stream = File.OpenRead(filePath);
        byte[] hash = await CryptographicOperations.HashDataAsync(
            new HashAlgorithmName(algorithm), stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }
}
