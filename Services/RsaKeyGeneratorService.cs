using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>RSA anahtar çifti üretir. Format: PEM / XML / Base64.</summary>
public sealed class RsaKeyGeneratorService : IRsaKeyGeneratorService
{
    private static readonly int[] Sizes = { 2048, 3072, 4096 };
    public int[] AllowedKeySizes => Sizes;

    public Task<ToolResult<RsaReport>> GenerateAsync(RsaOptions o, CancellationToken ct)
    {
        if (o is null || Array.IndexOf(Sizes, o.KeySize) < 0)
            return Fail<RsaReport>(ToolErrorKind.Validation, RsaKeyGeneratorErrorCodes.InvalidKeySize,
                $"Key size must be one of {string.Join(", ", Sizes)}.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            using var rsa = RSA.Create(o.KeySize);
            string pub, priv;

            switch (o.Format)
            {
                case RsaFormat.Xml:
                    pub = ExportXml(rsa, false);
                    priv = ExportXml(rsa, true);
                    break;
                case RsaFormat.Base64:
                    pub = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
                    priv = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
                    break;
                default: // Pem
                    pub = rsa.ExportSubjectPublicKeyInfoPem();
                    priv = rsa.ExportPkcs8PrivateKeyPem();
                    break;
            }

            sw.Stop();
            return Task.FromResult(ToolResult<RsaReport>.Ok(new RsaReport(pub, priv, o.KeySize, o.Format, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<RsaReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<RsaReport>(ToolErrorKind.Unexpected, RsaKeyGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    private static string ExportXml(RSA rsa, bool includePrivate)
    {
        var p = rsa.ExportParameters(includePrivate);
        var sb = new StringBuilder();
        sb.Append("<RSAKeyValue>");
        if (p.Modulus is not null) sb.Append($"<Modulus>{Convert.ToBase64String(p.Modulus)}</Modulus>");
        if (p.Exponent is not null) sb.Append($"<Exponent>{Convert.ToBase64String(p.Exponent)}</Exponent>");
        if (includePrivate)
        {
            if (p.P is not null) sb.Append($"<P>{Convert.ToBase64String(p.P)}</P>");
            if (p.Q is not null) sb.Append($"<Q>{Convert.ToBase64String(p.Q)}</Q>");
            if (p.DP is not null) sb.Append($"<DP>{Convert.ToBase64String(p.DP)}</DP>");
            if (p.DQ is not null) sb.Append($"<DQ>{Convert.ToBase64String(p.DQ)}</DQ>");
            if (p.InverseQ is not null) sb.Append($"<InverseQ>{Convert.ToBase64String(p.InverseQ)}</InverseQ>");
            if (p.D is not null) sb.Append($"<D>{Convert.ToBase64String(p.D)}</D>");
        }
        sb.Append("</RSAKeyValue>");
        return sb.ToString();
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}