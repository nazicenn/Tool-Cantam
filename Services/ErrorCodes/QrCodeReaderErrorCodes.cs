namespace tool_cantam.Services.ErrorCodes;

public static class QrCodeReaderErrorCodes
{
    public const string PathRequired = "L.QrCodeReader.Error.PathRequired";
    public const string FileNotFound = "L.QrCodeReader.Error.FileNotFound";
    public const string UnsupportedFormat = "L.QrCodeReader.Error.UnsupportedFormat";
    public const string FileTooLarge = "L.QrCodeReader.Error.FileTooLarge";
    public const string CorruptImage = "L.QrCodeReader.Error.CorruptImage";
    public const string NoQrFound = "L.QrCodeReader.Error.NoQrFound";
    public const string IoError = "L.QrCodeReader.Error.IoError";
    public const string Unexpected = "L.Common.Error.Unexpected";
}