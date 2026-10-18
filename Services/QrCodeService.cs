using System;
using System.Threading;
using System.Threading.Tasks;
using QRCoder;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// QRCoder (MIT) ile QR kod üretir.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    public const int MaxInputLengthLimit = 2048;
    public const int MinPixelsPerModule = 1;
    public const int MaxPixelsPerModule = 50;

    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<QrCodeReport>> GenerateAsync(
        string? input,
        QrCodeEcc ecc,
        int pixelsPerModule,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<QrCodeReport>(ToolErrorKind.Validation,
                QrCodeErrorCodes.EmptyInput, "Input is empty.");

        if (input.Length > MaxInputLengthLimit)
            return Fail<QrCodeReport>(ToolErrorKind.TooLarge,
                QrCodeErrorCodes.InputTooLong, $"Input length {input.Length} exceeds {MaxInputLengthLimit}.");

        if (pixelsPerModule < MinPixelsPerModule || pixelsPerModule > MaxPixelsPerModule)
            return Fail<QrCodeReport>(ToolErrorKind.Validation,
                QrCodeErrorCodes.InvalidParameters, $"PixelsPerModule {pixelsPerModule} out of range.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var level = ecc switch
            {
                QrCodeEcc.L => QRCodeGenerator.ECCLevel.L,
                QrCodeEcc.M => QRCodeGenerator.ECCLevel.M,
                QrCodeEcc.Q => QRCodeGenerator.ECCLevel.Q,
                _ => QRCodeGenerator.ECCLevel.H,
            };

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(input, level);
            using var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(pixelsPerModule);

            var report = new QrCodeReport(input, ecc, pixelsPerModule, bytes, bytes.Length);
            return Task.FromResult(ToolResult<QrCodeReport>.Ok(report));
        }
        catch (OperationCanceledException)
        {
            return Fail<QrCodeReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<QrCodeReport>(ToolErrorKind.Unexpected, QrCodeErrorCodes.Unexpected, ex.Message);
        }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}