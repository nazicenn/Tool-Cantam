using tool_cantam.Services.ErrorCodes;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Kriptografik özet hesaplama servisinin tek uygulaması.
/// <para>
/// Algoritma listesi bu makinede gerçekten çalışan algoritmalarla sınırlıdır; işletim sistemi bir
/// algoritmayı desteklemiyorsa kullanıcıya sunulmaz ve çağrıldığında açık hata döner.
/// MD5 ve SHA-1 <see cref="HashAlgorithmDescriptor.IsWeak"/> ile işaretlidir; arayüz bunu gösterir.
/// </para>
/// </summary>
public sealed class HashService : IHashService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    private const int StreamBufferSize = 81920;

    private static readonly HashAlgorithmDescriptor[] Candidates =
    {
        new(HashAlgorithmKind.Md5, "MD5", "MD5", 16, IsWeak: true),
        new(HashAlgorithmKind.Sha1, "SHA-1", "SHA1", 20, IsWeak: true),
        new(HashAlgorithmKind.Sha256, "SHA-256", "SHA256", 32, IsWeak: false),
        new(HashAlgorithmKind.Sha384, "SHA-384", "SHA384", 48, IsWeak: false),
        new(HashAlgorithmKind.Sha512, "SHA-512", "SHA512", 64, IsWeak: false),
        new(HashAlgorithmKind.Sha3_256, "SHA3-256", "SHA3-256", 32, IsWeak: false),
        new(HashAlgorithmKind.Sha3_512, "SHA3-512", "SHA3-512", 64, IsWeak: false)
    };

    private static readonly Lazy<HashAlgorithmDescriptor[]> AvailableAlgorithms = new(Probe, isThreadSafe: true);

    public int MaxTextLength => MaxTextLengthLimit;

    public IReadOnlyList<HashAlgorithmDescriptor> Algorithms => AvailableAlgorithms.Value;

    public ToolResult<byte[]> ComputeFromText(string? text, HashAlgorithmKind kind)
    {
        string input = text ?? string.Empty;

        if (input.Length > MaxTextLengthLimit)
        {
            return ToolResult<byte[]>.Fail(
                ToolErrorKind.TooLarge,
                HashErrorCodes.InputTooLarge,
                $"Input length {input.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        if (!TryResolve(kind, out HashAlgorithmDescriptor? descriptor, out ToolResult<byte[]>? failure))
            return failure!;

        byte[] bytes = Encoding.UTF8.GetBytes(input);

        try
        {
            byte[] digest = CryptographicOperations.HashData(new HashAlgorithmName(descriptor!.AlgorithmName), bytes);
            return ToolResult<byte[]>.Ok(digest);
        }
        catch (CryptographicException ex)
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Unexpected, HashErrorCodes.AlgorithmUnavailable, ex.Message);
        }
        finally
        {
            // Girdi hassas olabilir; ara tampon sıfırlanır (özet baytları çağırana aittir).
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public async Task<ToolResult<byte[]>> ComputeFromFileAsync(
        string filePath,
        HashAlgorithmKind kind,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Validation, HashErrorCodes.PathRequired, "File path is empty.");
        }

        if (!TryResolve(kind, out HashAlgorithmDescriptor? descriptor, out ToolResult<byte[]>? failure))
            return failure!;

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Validation, HashErrorCodes.PathRequired, ex.Message);
        }

        if (!File.Exists(fullPath))
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.NotFound, HashErrorCodes.FileNotFound, $"File not found: {fullPath}");
        }

        try
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                StreamBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            byte[] digest = await CryptographicOperations
                .HashDataAsync(new HashAlgorithmName(descriptor!.AlgorithmName), stream, cancellationToken)
                .ConfigureAwait(false);

            return ToolResult<byte[]>.Ok(digest);
        }
        catch (OperationCanceledException)
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Operation cancelled by the user.");
        }
        catch (CryptographicException ex)
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Unexpected, HashErrorCodes.AlgorithmUnavailable, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.IoError, HashErrorCodes.IoError, ex.Message);
        }
    }

    private bool TryResolve(HashAlgorithmKind kind, out HashAlgorithmDescriptor? descriptor, out ToolResult<byte[]>? failure)
    {
        failure = null;
        descriptor = null;

        foreach (HashAlgorithmDescriptor candidate in AvailableAlgorithms.Value)
        {
            if (candidate.Kind != kind)
                continue;

            descriptor = candidate;
            return true;
        }

        // Adaylar arasında var ama bu makinede çalışmıyorsa bunu ayırt etmek kullanıcı için önemli.
        foreach (HashAlgorithmDescriptor candidate in Candidates)
        {
            if (candidate.Kind != kind)
                continue;

            failure = ToolResult<byte[]>.Fail(
                ToolErrorKind.Unexpected,
                HashErrorCodes.AlgorithmUnavailable,
                $"{candidate.DisplayName} is not available on this system.");
            return false;
        }

        failure = ToolResult<byte[]>.Fail(
            ToolErrorKind.Validation,
            HashErrorCodes.UnsupportedAlgorithm,
            $"Unknown hash algorithm: {kind}.");
        return false;
    }

    private static HashAlgorithmDescriptor[] Probe()
    {
        var available = new List<HashAlgorithmDescriptor>(Candidates.Length);

        foreach (HashAlgorithmDescriptor candidate in Candidates)
        {
            try
            {
                CryptographicOperations.HashData(new HashAlgorithmName(candidate.AlgorithmName), ReadOnlySpan<byte>.Empty);
                available.Add(candidate);
            }
            catch (CryptographicException)
            {
                // Bu işletim sisteminde algoritma yok; listeye eklenmez.
            }
            catch (ArgumentException)
            {
                // Algoritma adı tanınmadı; listeye eklenmez.
            }
        }

        return available.ToArray();
    }
}
