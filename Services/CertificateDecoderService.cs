using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

/// <summary>X.509 sertifika decode (PEM / Base64 / DER / dosya).</summary>
public sealed class CertificateDecoderService : ICertificateDecoderService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    private static readonly Regex PemRegex = new(
        @"-----BEGIN CERTIFICATE-----(?<body>.*?)-----END CERTIFICATE-----",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public Task<ToolResult<CertificateReport>> DecodeAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<CertificateReport>(ToolErrorKind.Validation, CertificateDecoderErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<CertificateReport>(ToolErrorKind.TooLarge, "L.Common.Error.Unexpected", "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var cert = ParseCertificate(input);
            if (cert is null)
                return Fail<CertificateReport>(ToolErrorKind.InvalidFormat, CertificateDecoderErrorCodes.InvalidFormat, "Invalid certificate.");

            var report = BuildReport(cert, sw.Elapsed);
            return Task.FromResult(ToolResult<CertificateReport>.Ok(report));
        }
        catch (OperationCanceledException) { return Fail<CertificateReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<CertificateReport>(ToolErrorKind.InvalidFormat, CertificateDecoderErrorCodes.InvalidFormat, ex.Message); }
    }

    public Task<ToolResult<CertificateReport>> LoadFromFileAsync(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Fail<CertificateReport>(ToolErrorKind.Validation, CertificateDecoderErrorCodes.EmptyInput, "Path is empty.");
        if (!File.Exists(path))
            return Fail<CertificateReport>(ToolErrorKind.NotFound, CertificateDecoderErrorCodes.FileNotFound, "File not found.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var bytes = File.ReadAllBytes(path);
            var cert = X509CertificateLoader.LoadCertificate(bytes);
            var report = BuildReport(cert, sw.Elapsed);
            return Task.FromResult(ToolResult<CertificateReport>.Ok(report));
        }
        catch (OperationCanceledException) { return Fail<CertificateReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (IOException ex) { return Fail<CertificateReport>(ToolErrorKind.IoError, CertificateDecoderErrorCodes.IoError, ex.Message); }
        catch (Exception ex) { return Fail<CertificateReport>(ToolErrorKind.InvalidFormat, CertificateDecoderErrorCodes.InvalidFormat, ex.Message); }
    }

    private static X509Certificate2? ParseCertificate(string input)
    {
        var m = PemRegex.Match(input);
        if (m.Success)
        {
            var base64 = Regex.Replace(m.Groups["body"].Value, @"\s", "");
            try { return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64)); }
            catch { return null; }
        }
        try { return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(input.Trim())); }
        catch { return null; }
    }

    private static CertificateReport BuildReport(X509Certificate2 cert, TimeSpan elapsed)
    {
        int keySize = 0;
        using (var rsa = cert.GetRSAPublicKey()) if (rsa is not null) keySize = rsa.KeySize;
        if (keySize == 0) using (var ec = cert.GetECDsaPublicKey()) if (ec is not null) keySize = ec.KeySize;

        var exts = cert.Extensions.Cast<X509Extension>()
            .Select(e => new CertExtension(
                e.Oid?.FriendlyName ?? e.Oid?.Value ?? "Unknown",
                e.Format(true) ?? ""))
            .ToList();

        var now = DateTime.Now;
        return new CertificateReport(
            $"V{cert.Version}",
            cert.SerialNumber ?? "N/A",
            cert.SignatureAlgorithm?.FriendlyName ?? "N/A",
            cert.Issuer ?? "N/A",
            cert.Subject ?? "N/A",
            cert.NotBefore, cert.NotAfter,
            now >= cert.NotBefore && now <= cert.NotAfter,
            cert.PublicKey?.Oid?.FriendlyName ?? "N/A",
            keySize,
            cert.Thumbprint ?? "N/A",
            exts, elapsed);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}