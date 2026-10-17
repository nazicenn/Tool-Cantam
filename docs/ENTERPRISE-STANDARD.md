# Kurumsal Seviye Standardı ve İlerleme

Bu belge, her aracı tek tek kurumsal seviyeye taşırken uygulanan standardı ve ilerlemeyi tutar.
Referans uygulama: **Base64 Codec** (`Services/Base64Service.cs`, `ViewModels/Base64ViewModel.cs`,
`Views/Base64Page.xaml`, `tests/tool-cantam.Tests/Base64*Tests.cs`).

## Her araç için zorunlu standart

Bir araç "kurumsal" sayılmak için bu maddelerin **tamamını** karşılar. Referans sütunu,
standardın Base64 Codec'te nasıl uygulandığını gösterir.

| # | Kural | Referans |
|---|---|---|
| 1 | İş mantığı arayüzlü serviste; ViewModel ince, yalnız sunum/komut | `IBase64Service` / `Base64Service` |
| 2 | Sonuç modeli: istisna yerine `ToolResult<T>` + `ToolErrorKind`; boş `catch` yok | `Core/Models/ToolResult.cs` |
| 3 | Hata metni koda gömülü değil: hizmet yerelleştirme anahtarı döner | `Base64ErrorCodes` |
| 4 | Uzun işlemler async + `CancellationToken` + iptal komutu; UI bloklanmaz | `[RelayCommand(IncludeCancelCommand = true)]` |
| 5 | Boyut/girdi sınırları ve bellek koruması; sessiz veri bozulması yok | 10 MB sınırı, katı UTF-8 |
| 6 | Yapılandırılmış log (`ILogger<T>`): işlem, boyut, süre, sonuç — **içerik loglanmaz** | `Base64ViewModel` |
| 7 | Kullanıcıya metin `ILocalizerService` ile; `MessageBox` ve ham `ex.Message` yok | `L.Base64.*` anahtarları |
| 8 | Dosya işlemleri atomik: geçici dosya + taşıma, hata/iptalde yarım çıktı yok | `TransformFileAsync` |
| 9 | Görünüm: Tab sırası, `AutomationProperties`, durum rengi, iptal düğmesi, tema kaynakları | `Base64Page.xaml` |
| 10 | Otomatik testler: mutlu yol, sınır değer, hata yolu ve kaynak temizliği | `Base64ServiceTests`, `Base64ViewModelTests` |

Ek kurallar:

- Kültürden bağımsız çıktı: `InvariantCulture`, gerekirse kullanıcı yereli ayrıca kabul edilir.
- Yerelleştirme dosyaları (`en-US.xaml`, `tr-TR.xaml`) anahtar kümesi bakımından **birebir** olmalıdır; testle doğrulanır.
- Ölü kod bırakılmaz: kullanılmayan sözleşme, alan veya delege silinir.

## Faz 0 — Ortak omurga durumu

| Bileşen | Durum |
|---|---|
| Test projesi (xunit v3 + Microsoft.Testing.Platform, `tests/tool-cantam.Tests`) | ✅ 279 test geçiyor |
| Sonuç/hata modeli (`ToolResult<T>`, `ToolErrorKind`) | ✅ |
| Yerelleştirme altyapısı (`ILocalizerService`, dil kodu eşlemesi, anahtar parite testi) | ✅ (dil değiştirme hatası düzeltildi) |
| Yerelleştirme anahtarı denetimi (görünüm ve ViewModel'lerde kullanılan anahtarlar) | ✅ testle korunuyor |
| Bildirim servisi (DI'da çözülemeyen yapıcı düzeltildi) | ✅ |
| Çözüm derlemesi | ⚠️ Paralel çözüm derlemesi bu ortamda yarışıyor; `dotnet build -m:1` ile geçiyor |
| CI (GitHub Actions), `.editorconfig`, analizör seti | ⬜ bekliyor |
| `README` / `CHANGELOG` / `LICENSE` | ⬜ bekliyor |

## Araç sırası ve durum

Sıra, uygulamadaki araç listesiyle (kayıt sırası) aynıdır; işaretlenen araç standardın tamamını karşılar.

| # | Araç | Durum |
|---|---|---|
| 1 | Base64 Codec | ✅ tamam |
| 2 | URL Encoder/Decoder | ✅ tamam |
| 3 | Hex Encoder/Decoder | ✅ tamam |
| 4 | HTML Encoder/Decoder | ⬜ |
| 5 | Binary Encoder/Decoder | ⬜ |
| 6 | ROT13 Cipher | ⬜ |
| 7 | ASCII Converter | ⬜ |
| 8 | Unicode Converter | ⬜ |
| 9 | Hash Generator | ⬜ |
| 10 | Hash Identifier | ⬜ |
| 11 | HMAC Generator | ⬜ |
| 12 | Port Scanner | ⬜ |
| 13 | Subdomain Scanner | ⬜ |
| 14 | DNS Lookup | ⬜ |
| 15 | Reverse DNS | ⬜ |
| 16 | IP Info | ⬜ |
| 17 | WHOIS Lookup | ⬜ |
| 18 | MAC Lookup | ⬜ |
| 19 | SSL Checker | ⬜ |
| 20 | Ping Test | ⬜ |
| 21 | Case Converter | ⬜ |
| 22 | Text Counter | ⬜ |
| 23 | String Reverser | ⬜ |
| 24 | JSON Formatter | ⬜ |
| 25 | JWT Decoder | ⬜ |
| 26 | Password Generator | ⬜ |
| 27 | Metadata Cleaner | ⬜ |
| 28 | Unix Timestamp | ⬜ |
| 29 | QR Code Generator | ⬜ |
| 30 | HTTP Header Analyzer | ⬜ |
| 31 | Text Sorter | ⬜ |
| 32 | UUID Generator | ⬜ |
| 33 | Random String Generator | ⬜ |
| 34 | Slug Generator | ⬜ |
| 35 | Find and Replace | ⬜ |
| 36 | Regex Tester | ⬜ |
| 37 | Text Diff | ⬜ |
| 38 | SQL Formatter | ⬜ |
| 39 | XML Formatter | ⬜ |
| 40 | CSV to JSON | ⬜ |
| 41 | JWT Generator | ⬜ |
| 42 | Cron Parser | ⬜ |
| 43 | AES Encrypt/Decrypt | ⬜ |
| 44 | RSA Key Generator | ⬜ |
| 45 | HTTP Request Tester | ⬜ |
| 46 | Bcrypt Generator | ⬜ |
| 47 | IP Calculator | ⬜ |
| 48 | EXIF Viewer | ⬜ |
| 49 | cURL Generator | ⬜ |
| 50 | Secret Generator | ⬜ |
| 51 | GeoIP Lookup | ⬜ |
| 52 | YAML Formatter | ⬜ |
| 53 | JSON Minifier | ⬜ |
| 54 | Fake Data Generator | ⬜ |
| 55 | CSS Gradient Generator | ⬜ |
| 56 | MX Lookup | ⬜ |
| 57 | NS Lookup | ⬜ |
| 58 | Certificate Decoder | ⬜ |
| 59 | PEM Viewer | ⬜ |
| 60 | Traceroute | ⬜ |
| 61 | Base64 Image Converter | ⬜ |
| 62 | RGB Color Converter | ⬜ |
| 63 | Lorem Ipsum Generator | ⬜ |
| 64 | Word Counter Pro | ⬜ |
| 65 | Unit Converter | ⬜ |

> Not: `ColorPicker`, `Markdown`, `Pwned`, `QrCodeReader`, `RestApiClient`, `UserAgentParser`,
> `YamlToJson` araçlarının ViewModel ve görünümleri var ama araç kaydında bulunmadıkları için
> arayüzden erişilemiyor; ilgili adımda kayda bağlanacaklar.
