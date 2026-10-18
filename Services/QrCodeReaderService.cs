using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using ZXing;

namespace tool_cantam.Services;

/// <summary>
/// ZXing.Net ile QR kod okur. Dosya kilidi bırakılır; bitmap bellekten okunur.
/// System.Drawing gerekmez — BitmapImage doğrudan LuminanceSource'a çevrilir.
/// </summary>
public sealed class QrCodeReaderService : IQrCodeReaderService
{
    public const long MaxFileSizeLimit = 20L * 1024 * 1024; // 20 MB

    private static readonly string[] Extensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    public long MaxFileSize => MaxFileSizeLimit;
    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public Task<ToolResult<QrCodeReadReport>> ReadAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Fail<QrCodeReadReport>(ToolErrorKind.Validation,
                QrCodeReaderErrorCodes.PathRequired, "Path is null or empty.");

        if (!File.Exists(path))
            return Fail<QrCodeReadReport>(ToolErrorKind.NotFound,
                QrCodeReaderErrorCodes.FileNotFound, $"File not found: {path}");

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!Extensions.Contains(ext))
            return Fail<QrCodeReadReport>(ToolErrorKind.InvalidFormat,
                QrCodeReaderErrorCodes.UnsupportedFormat, $"Unsupported extension: {ext}");

        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxFileSizeLimit)
                return Fail<QrCodeReadReport>(ToolErrorKind.TooLarge,
                    QrCodeReaderErrorCodes.FileTooLarge, $"File size {info.Length} exceeds {MaxFileSizeLimit}.");

            cancellationToken.ThrowIfCancellationRequested();

            // BitmapImage'i bellekten yükle → dosya kilidi bırakılsın
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            // ZXing ile oku
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                }
            };

            var source = new BitmapImageLuminanceSource(bitmap);
            var result = reader.Decode(source);

            if (result is null)
                return Fail<QrCodeReadReport>(ToolErrorKind.NotFound,
                    QrCodeReaderErrorCodes.NoQrFound, "No QR code detected in image.");

            var report = new QrCodeReadReport(path, result.Text, result.BarcodeFormat.ToString(), info.Length);
            return Task.FromResult(ToolResult<QrCodeReadReport>.Ok(report));
        }
        catch (OperationCanceledException)
        {
            return Fail<QrCodeReadReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (NotSupportedException ex)
        {
            return Fail<QrCodeReadReport>(ToolErrorKind.InvalidFormat,
                QrCodeReaderErrorCodes.CorruptImage, ex.Message);
        }
        catch (FileFormatException ex)
        {
            return Fail<QrCodeReadReport>(ToolErrorKind.InvalidFormat,
                QrCodeReaderErrorCodes.CorruptImage, ex.Message);
        }
        catch (IOException ex)
        {
            return Fail<QrCodeReadReport>(ToolErrorKind.IoError,
                QrCodeReaderErrorCodes.IoError, ex.Message);
        }
        catch (Exception ex)
        {
            return Fail<QrCodeReadReport>(ToolErrorKind.Unexpected,
                QrCodeReaderErrorCodes.Unexpected, ex.Message);
        }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}

/// <summary>
/// WPF BitmapImage → ZXing LuminanceSource köprüsü.
/// ZXing.Net Java'dan port edildiği için metot adı küçük harfle 'getRow'dur.
/// </summary>
internal sealed class BitmapImageLuminanceSource : ZXing.LuminanceSource
{
    private readonly byte[] _luminances;
    private readonly int _width;
    private readonly int _height;

    public BitmapImageLuminanceSource(BitmapImage bitmap)
        : base(bitmap.PixelWidth, bitmap.PixelHeight)
    {
        _width = bitmap.PixelWidth;
        _height = bitmap.PixelHeight;

        // Bgra32'ye çevir
        var converted = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        converted.Freeze();

        int stride = (_width * 4 + 3) & ~3;
        var pixels = new byte[stride * _height];
        converted.CopyPixels(pixels, stride, 0);

        _luminances = new byte[_width * _height];
        for (int y = 0; y < _height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < _width; x++)
            {
                int i = rowOffset + x * 4;
                byte b = pixels[i + 0];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                _luminances[y * _width + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
            }
        }
    }

    // ⚠️ ZXing.Net: metot adı 'getRow' (küçük harf) — override imzası buna uymalı.
    public override byte[] getRow(int y, byte[] row)
    {
        if (row is null || row.Length < _width)
            row = new byte[_width];
        Array.Copy(_luminances, y * _width, row, 0, _width);
        return row;
    }

    // ZXing.Net: 'Matrix' yerine 'matrix' (küçük harf) property'si de olabilir;
    // abstract üye hangisini istiyorsa override edilmeli. Aşağıdaki ikisinden biri yeterli:
    public override byte[] Matrix => _luminances;

    // Bazı sürümlerde 'matrix' küçük harf abstract property'si var; onu da override et:
    // (CS0115/CS0534 hatası hâlâ çıkarsa bu satırı aç.)
    // public override byte[] matrix => _luminances;
}