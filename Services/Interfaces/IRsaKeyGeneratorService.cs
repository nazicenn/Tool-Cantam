using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IRsaKeyGeneratorService
{
    int[] AllowedKeySizes { get; }
    Task<ToolResult<RsaReport>> GenerateAsync(RsaOptions options, CancellationToken ct);
}

public enum RsaFormat { Pem, Xml, Base64 }

public sealed record RsaOptions(int KeySize, RsaFormat Format);
public sealed record RsaReport(string PublicKey, string PrivateKey, int KeySize, RsaFormat Format, TimeSpan Elapsed);