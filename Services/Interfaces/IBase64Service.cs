using System.Threading;
using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Base64 kodlama/çözme servis arayüzü.
/// Metin ve dosya tabanlı işlemleri tanımlar.
/// Referans: https://learn.microsoft.com/en-us/dotnet/api/system.convert.tobase64string
/// </summary>
public interface IBase64Service
{
    string EncodeText(string plainText);
    string DecodeText(string base64Text);
    Task<bool> EncodeFileAsync(string filePath, string outputPath, CancellationToken cancellationToken = default);
    Task<bool> DecodeFileAsync(string filePath, string outputPath, CancellationToken cancellationToken = default);
}
