namespace tool_cantam.Core.Enums;

/// <summary>
/// Base64 alfabesi. <see cref="Standard"/> RFC 4648 §4, <see cref="UrlSafe"/> RFC 4648 §5
/// (URL ve dosya adı güvenli) alfabesidir; URL-safe biçimde dolgu karakteri kullanılmaz.
/// </summary>
public enum Base64Variant
{
    Standard,
    UrlSafe
}
