using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>PEM parse — sertifika + tüm PEM bloklarını algılar.</summary>
public sealed class PemViewerService : IPemViewerService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    private static readonly Regex BlockRegex = new(
        @"-----BEGIN (?<label>[^-]+)-----(?<body>.*?)-----END (?<endlabel>[^-]+)-----",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public Task<ToolResult<PemReport>> ParseAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<PemReport>(ToolErrorKind.Validation, PemViewerErrorCodes.EmptyInput, "Input is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var blocks = new List<PemBlock>();
            X509Certificate2? cert = null;

            foreach (Match m in BlockRegex.Matches(input))
            {
                ct.ThrowIfCancellationRequested();
                var label = m.Groups["label"].Value.Trim();
                var body = Regex.Replace(m.Groups["body"].Value, @"\s", "");

                byte[] bytes;
                try { bytes = Convert.FromBase64String(body); }
                catch { continue; }

                blocks.Add(new PemBlock(label, bytes.Length));

                if (label == "CERTIFICATE" && cert is null)
                {
                    try { cert = X509CertificateLoader.LoadCertificate(bytes); } catch { }
                }
            }

            if (cert is null)
                return Fail<PemReport>(ToolErrorKind.InvalidFormat, PemViewerErrorCodes.InvalidPem, "No valid certificate block found.");

            var now = DateTime.Now;
            var report = new PemReport(
                cert.Subject ?? "N/A",
                cert.Issuer ?? "N/A",
                cert.SerialNumber ?? "N/A",
                cert.NotBefore, cert.NotAfter,
                now >= cert.NotBefore && now <= cert.NotAfter,
                cert.PublicKey?.Oid?.FriendlyName ?? "N/A",
                cert.SignatureAlgorithm?.FriendlyName ?? "N/A",
                cert.Thumbprint ?? "N/A",
                blocks, sw.Elapsed);

            return Task.FromResult(ToolResult<PemReport>.Ok(report));
        }
        catch (OperationCanceledException) { return Fail<PemReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<PemReport>(ToolErrorKind.InvalidFormat, PemViewerErrorCodes.InvalidPem, ex.Message); }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}