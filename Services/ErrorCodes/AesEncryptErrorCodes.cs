namespace tool_cantam.Services.ErrorCodes;

public static class AesEncryptErrorCodes
{
    public const string EmptyInput = "L.AesEncrypt.Error.EmptyInput";
    public const string EmptyPassword = "L.AesEncrypt.Error.EmptyPassword";
    public const string PasswordTooShort = "L.AesEncrypt.Error.PasswordTooShort";
    public const string InvalidBase64 = "L.AesEncrypt.Error.InvalidBase64";
    public const string InvalidCipher = "L.AesEncrypt.Error.InvalidCipher";
    public const string Unexpected = "L.Common.Error.Unexpected";
}