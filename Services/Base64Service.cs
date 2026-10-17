using System.IO;
using System.Security.Cryptography;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Base64 kodlama/çözme işlemlerinin tek uygulaması.
/// <para>
/// Kurallar: boşluk ve satır sonu karakterleri yok sayılır; dolgu karakteri eksikse tamamlanır;
/// karakter kümesi seçilen alfabeye göre katı biçimde doğrulanır (yanlış alfabedeki karakter reddedilir);
/// çözülen içerik geçerli UTF-8 değilse hata döner (sessiz bozulma yok).
/// </para>
/// </summary>
public sealed class Base64Service : IBase64Service
{
    /// <summary>Metin işlemlerinde izin verilen en büyük UTF-8 bayt sayısı (10 MB).</summary>
    public const int MaxTextBytesLimit = 10 * 1024 * 1024;

    private const int StreamBufferSize = 81920;

    public int MaxTextBytes => MaxTextBytesLimit;

    public ToolResult<string> EncodeText(string plainText, Base64Variant variant = Base64Variant.Standard)
    {
        if (string.IsNullOrEmpty(plainText))
            return ToolResult<string>.Ok(string.Empty);

        byte[] bytes = Encoding.UTF8.GetBytes(plainText);

        if (bytes.Length > MaxTextBytesLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                Base64ErrorCodes.InputTooLarge,
                $"UTF-8 input is {bytes.Length} bytes; limit is {MaxTextBytesLimit}.");
        }

        string encoded = Convert.ToBase64String(bytes);
        return ToolResult<string>.Ok(ToVariant(encoded, variant));
    }

    public ToolResult<string> DecodeText(string base64Text, Base64Variant variant = Base64Variant.Standard)
    {
        if (string.IsNullOrWhiteSpace(base64Text))
            return ToolResult<string>.Ok(string.Empty);

        if (!TryNormalize(base64Text, variant, out string normalized, out ToolResult<string>? failure))
            return failure!;

        // Bellek koruması: çözülecek yaklaşık boyutu ayırma yapmadan önce sınırla.
        long expectedBytes = (long)(normalized.Length / 4) * 3;
        if (expectedBytes > MaxTextBytesLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                Base64ErrorCodes.OutputTooLarge,
                $"Decoded size would be about {expectedBytes} bytes; limit is {MaxTextBytesLimit}.");
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(normalized);
        }
        catch (FormatException ex)
        {
            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, Base64ErrorCodes.InvalidFormat, ex.Message);
        }

        if (decoded.Length > MaxTextBytesLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                Base64ErrorCodes.OutputTooLarge,
                $"Decoded size is {decoded.Length} bytes; limit is {MaxTextBytesLimit}.");
        }

        try
        {
            return ToolResult<string>.Ok(TextEncodings.StrictUtf8.GetString(decoded));
        }
        catch (DecoderFallbackException ex)
        {
            // İkili veri metin aracıyla çözülmeye çalışılıyor: bozuk metin yerine açık hata.
            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, Base64ErrorCodes.NotUtf8Text, ex.Message);
        }
    }

    public async Task<ToolResult<long>> EncodeFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (!TryPreparePaths(sourcePath, destinationPath, out string source, out string destination, out ToolResult<long>? pathFailure))
            return pathFailure!;

        return await TransformFileAsync(
            source,
            destination,
            isEncoding: true,
            Base64ErrorCodes.IoError,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ToolResult<long>> DecodeFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (!TryPreparePaths(sourcePath, destinationPath, out string source, out string destination, out ToolResult<long>? pathFailure))
            return pathFailure!;

        // Satır sonu içeren Base64 dosyaları (PEM, e-posta) standart dönüşümle çözülemez.
        return await TransformFileAsync(
            source,
            destination,
            isEncoding: false,
            Base64ErrorCodes.InvalidFileContent,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Alfabeye göre karakter doğrulaması yapar, boşlukları atar ve dolguyu 4'ün katına tamamlar.
    /// </summary>
    private static bool TryNormalize(string input, Base64Variant variant, out string normalized, out ToolResult<string>? failure)
    {
        failure = null;
        normalized = string.Empty;

        var buffer = new StringBuilder(input.Length);

        for (int index = 0; index < input.Length; index++)
        {
            char character = input[index];

            if (char.IsWhiteSpace(character))
                continue;

            if (character == '=')
            {
                buffer.Append(character);
                continue;
            }

            if (variant == Base64Variant.UrlSafe)
            {
                if (character == '-') character = '+';
                else if (character == '_') character = '/';
            }

            bool isBase64Character =
                (character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9') ||
                character == '+' || character == '/';

            if (!isBase64Character)
            {
                failure = ToolResult<string>.Fail(
                    ToolErrorKind.InvalidFormat,
                    Base64ErrorCodes.InvalidCharacter,
                    $"Unexpected character '{input[index]}' (U+{(int)input[index]:X4}) at index {index}.");
                return false;
            }

            buffer.Append(character);
        }

        string cleaned = buffer.ToString();
        string body = cleaned.TrimEnd('=');
        int padding = cleaned.Length - body.Length;

        if (padding > 2 || body.Contains('='))
        {
            failure = ToolResult<string>.Fail(
                ToolErrorKind.InvalidFormat,
                Base64ErrorCodes.InvalidPadding,
                $"Invalid padding in input of length {cleaned.Length}.");
            return false;
        }

        int remainder = body.Length % 4;
        if (remainder == 1)
        {
            failure = ToolResult<string>.Fail(
                ToolErrorKind.InvalidFormat,
                Base64ErrorCodes.InvalidLength,
                $"Base64 payload length {body.Length} cannot form a complete block.");
            return false;
        }

        normalized = remainder == 0 ? body : body + new string('=', 4 - remainder);
        return true;
    }

    private static string ToVariant(string encoded, Base64Variant variant) => variant switch
    {
        // RFC 7515 (JWT) uyumu: URL-safe alfabede dolgu kullanılmaz.
        Base64Variant.UrlSafe => encoded.Replace('+', '-').Replace('/', '_').TrimEnd('='),
        _ => encoded
    };

    private static bool TryPreparePaths(string sourcePath, string destinationPath, out string source, out string destination, out ToolResult<long>? failure)
    {
        source = string.Empty;
        destination = string.Empty;
        failure = null;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            failure = ToolResult<long>.Fail(ToolErrorKind.Validation, Base64ErrorCodes.PathRequired, "Source path is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            failure = ToolResult<long>.Fail(ToolErrorKind.Validation, Base64ErrorCodes.PathRequired, "Destination path is empty.");
            return false;
        }

        source = Path.GetFullPath(sourcePath);
        destination = Path.GetFullPath(destinationPath);

        if (!File.Exists(source))
        {
            failure = ToolResult<long>.Fail(ToolErrorKind.NotFound, Base64ErrorCodes.FileNotFound, $"Source file not found: {source}");
            return false;
        }

        try
        {
            string? directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            failure = ToolResult<long>.Fail(ToolErrorKind.IoError, Base64ErrorCodes.IoError, ex.Message);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Dönüşümü geçici dosyaya yazar, başarılıysa hedefe taşır. Böylece hata/iptal durumunda
    /// yarım çıktı dosyası kalmaz ve mevcut hedef dosya bozulmaz.
    /// Kodlamada hedef akış, çözmede kaynak akış dönüşümün içinden geçer.
    /// </summary>
    private static async Task<ToolResult<long>> TransformFileAsync(
        string source,
        string destination,
        bool isEncoding,
        string invalidContentErrorCode,
        CancellationToken cancellationToken)
    {
        string tempPath = destination + ".tmp";

        try
        {
            long written;

            await using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var destinationStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.Asynchronous))
            {
                if (isEncoding)
                {
                    // ToBase64Transform yalnızca yazma modunda kullanılabilir; kapanışta kalan blok yazılır.
                    await using (var encoder = new CryptoStream(destinationStream, new ToBase64Transform(), CryptoStreamMode.Write, leaveOpen: true))
                    {
                        await sourceStream.CopyToAsync(encoder, StreamBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    await using (var decoder = new CryptoStream(sourceStream, new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces), CryptoStreamMode.Read, leaveOpen: true))
                    {
                        await decoder.CopyToAsync(destinationStream, StreamBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            written = new FileInfo(tempPath).Length;
            File.Move(tempPath, destination, overwrite: true);

            return ToolResult<long>.Ok(written);
        }
        catch (OperationCanceledException)
        {
            return ToolResult<long>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Operation cancelled by the user.");
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return ToolResult<long>.Fail(ToolErrorKind.InvalidFormat, invalidContentErrorCode, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return ToolResult<long>.Fail(ToolErrorKind.IoError, Base64ErrorCodes.IoError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<long>.Fail(ToolErrorKind.Unexpected, Base64ErrorCodes.Unexpected, ex.ToString());
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Geçici dosya temizliği başarısız olsa da işlem sonucu değişmez; çağıran loglar.
        }
    }
}
