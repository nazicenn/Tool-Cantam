namespace tool_cantam.Core.Contracts;

/// <summary>
/// Arayüz metinlerinin ve aktif dilin tek erişim noktası.
/// ViewModel'ler bu servisi enjekte eder; böylece metinler test edilebilir ve dilden bağımsızdır.
/// </summary>
public interface ILocalizerService
{
    /// <summary>Aktif dil kodu (ör. <c>en-US</c>, <c>tr-TR</c>).</summary>
    string CurrentCulture { get; }

    /// <summary>Desteklenen dil kodları.</summary>
    IReadOnlyCollection<string> AvailableCultures { get; }

    /// <summary>
    /// Anahtarı aktif dildeki metne çevirir. Anahtar bulunamazsa anahtarın kendisi döner;
    /// böylece eksik çeviri sessizce boş metne dönüşmez, ekranda fark edilir.
    /// </summary>
    string Get(string key);

    /// <summary>Anahtarı çözüp <see cref="string.Format(string, object?[])"/> ile biçimlendirir.</summary>
    string Format(string key, params object?[] args);

    /// <summary>Aktif dili değiştirir. Desteklenmeyen kod veya kaynak yükleme hatasında false döner.</summary>
    bool SetCulture(string cultureCode);
}
