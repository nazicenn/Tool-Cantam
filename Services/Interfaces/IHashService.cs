using System.Threading;
using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Kriptografik hash hesaplama servis arayüzü.
/// Desteklenen algoritmalar: MD5, SHA1, SHA256, SHA384, SHA512.
/// Referans: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.hashdata
/// </summary>
public interface IHashService
{
    string ComputeFromText(string input, string algorithm);
    Task<string> ComputeFromFileAsync(string filePath, string algorithm, CancellationToken cancellationToken = default);
}
