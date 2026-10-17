using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// TCP port tarama sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner; iptal, hedef
/// çözümleme hatası ve geçersiz aralık sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IPortScannerService
{
    /// <summary>Tek taramada izin verilen en fazla port sayısı (kurumsal kötüye kullanım sınırı).</summary>
    int MaxPortsPerScan { get; }

    /// <summary>Aynı anda açılabilecek en fazla bağlantı sayısı.</summary>
    int MaxConcurrency { get; }

    /// <summary>İzin verilen en büyük zaman aşımı (milisaniye).</summary>
    int MaxTimeoutMs { get; }

    /// <summary>
    /// Belirtilen aralığı tarar. Host adı <b>bir kez</b> çözümlenir, ardından doğrudan IP adresine
    /// bağlanılır; böylece port sayısı kadar DNS sorgusu yapılmaz.
    /// </summary>
    Task<ToolResult<PortScanReport>> ScanAsync(
        string? target,
        int startPort,
        int endPort,
        int timeoutMs,
        IProgress<PortScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Port tarayıcının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class PortScanErrorCodes
{
    public const string InvalidTarget = "L.PortScan.Error.InvalidTarget";
    public const string TargetNotFound = "L.PortScan.Error.TargetNotFound";
    public const string InvalidRange = "L.PortScan.Error.InvalidRange";
    public const string RangeTooLarge = "L.PortScan.Error.RangeTooLarge";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
