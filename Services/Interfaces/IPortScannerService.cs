using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;

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
