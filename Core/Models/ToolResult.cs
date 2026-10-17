namespace tool_cantam.Core.Models;

/// <summary>
/// Bir araç işleminin başarısızlık sınıfı. Sunum katmanı bu sınıfa göre karar verir;
/// kullanıcıya gösterilecek metin <see cref="IToolResult.ErrorCode"/> üzerinden yerelleştirilir.
/// </summary>
public enum ToolErrorKind
{
    None,
    /// <summary>Eksik veya geçersiz kullanıcı girdisi.</summary>
    Validation,
    /// <summary>Girdinin biçimi bozuk (ör. geçersiz Base64, bozuk JSON).</summary>
    InvalidFormat,
    /// <summary>Boyut/uzunluk sınırı aşıldı.</summary>
    TooLarge,
    /// <summary>Dosya veya kayıt bulunamadı.</summary>
    NotFound,
    /// <summary>Kullanıcı işlemi iptal etti.</summary>
    Cancelled,
    /// <summary>Dosya sistemi hatası.</summary>
    IoError,
    /// <summary>Ağ hatası.</summary>
    Network,
    /// <summary>Beklenmeyen hata; teknik ayrıntı yalnızca logda tutulur.</summary>
    Unexpected
}

/// <summary>
/// Sonuç tipinin ortak yüzü; çağıran katman sonucun türünden bağımsız olarak hata bilgisine erişebilir.
/// </summary>
public interface IToolResult
{
    bool IsSuccess { get; }
    ToolErrorKind ErrorKind { get; }

    /// <summary>
    /// Yerelleştirme anahtarı (ör. <c>L.Base64.Error.InvalidCharacter</c>).
    /// Aynı değer logda makine tarafından okunabilir hata kodu olarak da kullanılır.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>Yalnızca log/teşhis için teknik ayrıntı. Kullanıcıya ham biçimde gösterilmez.</summary>
    string? TechnicalDetail { get; }
}

/// <summary>
/// Bir araç işleminin sonucu: başarı durumunda değer, başarısızlık durumunda sınıflandırılmış hata taşır.
/// İstisna fırlatmak yerine sonuç döndürmek, her aracın aynı hata yolunu izlemesini sağlar.
/// </summary>
public sealed record ToolResult<T> : IToolResult
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public ToolErrorKind ErrorKind { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? TechnicalDetail { get; private init; }

    /// <summary>İşlemin ölçülen süresi; telemetri ve kullanıcı geri bildirimi için.</summary>
    public TimeSpan Elapsed { get; private init; }

    public static ToolResult<T> Ok(T value, TimeSpan elapsed = default) => new()
    {
        IsSuccess = true,
        Value = value,
        ErrorKind = ToolErrorKind.None,
        Elapsed = elapsed
    };

    public static ToolResult<T> Fail(ToolErrorKind kind, string errorCode, string? technicalDetail = null, TimeSpan elapsed = default) => new()
    {
        IsSuccess = false,
        ErrorKind = kind,
        ErrorCode = errorCode,
        TechnicalDetail = technicalDetail,
        Elapsed = elapsed
    };
}
