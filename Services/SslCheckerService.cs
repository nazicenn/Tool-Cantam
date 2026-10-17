using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// SSL sertifika sorgusunun tek uygulaması. TcpClient + SslStream ile TLS el sıkışması yapar,
/// sertifika zincirini doğrular ve sonucu sınıflandırılmış rapor olarak döndürür.
/// </summary>
public sealed class SslCheckerService : ISslCheckerService
{
    public const int QueryTimeoutMsLimit = 10000;

    public int QueryTimeoutMs => QueryTimeoutMsLimit;

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                SslCheckerErrorCodes.InvalidDomain,
                "Domain is empty.");

        string trimmed = domain.Trim().ToLowerInvariant();

        if (trimmed.Length > 253)
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                SslCheckerErrorCodes.InvalidDomain,
                $"Domain is too long ({trimmed.Length} characters).");

        if (trimmed.Contains(' ') || trimmed.Contains('/') || trimmed.Contains(':'))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                SslCheckerErrorCodes.InvalidDomain,
                $"Domain contains invalid characters: '{trimmed}'.");

        if (trimmed.Contains('_'))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                SslCheckerErrorCodes.InvalidDomain,
                $"Domain contains underscore: '{trimmed}'.");

        if (Uri.CheckHostName(trimmed) == UriHostNameType.Unknown)
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                SslCheckerErrorCodes.InvalidDomain,
                $"'{trimmed}' is not a valid domain.");

        return ToolResult<string>.Ok(trimmed);
    }

    public async Task<ToolResult<SslCertificateReport>> CheckAsync(
        string? domain,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateDomain(domain);
        if (!validation.IsSuccess)
            return ToolResult<SslCertificateReport>.Fail(
                validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string target = validation.Value!;
        var stopwatch = Stopwatch.StartNew();
        SslPolicyErrors policyErrors = SslPolicyErrors.None;

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(QueryTimeoutMsLimit);

            using var client = new TcpClient();
            await client.ConnectAsync(target, 443, timeoutSource.Token).ConfigureAwait(false);

            using var sslStream = new SslStream(
                client.GetStream(),
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (sender, certificate, chain, errors) =>
                {
                    policyErrors = errors;
                    return true; // Ayrıntıları raporlamak için geçici olarak true
                });

            var authOptions = new SslClientAuthenticationOptions
            {
                TargetHost = target
            };

            await sslStream.AuthenticateAsClientAsync(authOptions, timeoutSource.Token).ConfigureAwait(false);

            stopwatch.Stop();

            if (sslStream.RemoteCertificate is not X509Certificate2 cert)
            {
                return ToolResult<SslCertificateReport>.Fail(
                    ToolErrorKind.NotFound,
                    SslCheckerErrorCodes.NoCertificate,
                    "Remote server did not present a certificate.");
            }

            var now = DateTime.Now;
            bool isExpired = now > cert.NotAfter;
            bool isNotYetValid = now < cert.NotBefore;

            bool isHostnameMatch = (policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == 0;

            bool isChainTrusted = ValidateChain(cert, out var chainIssues);

            bool isValid = !isExpired && !isNotYetValid && isHostnameMatch && isChainTrusted;

            string status = isValid ? "Valid"
                : isExpired ? "Expired"
                : isNotYetValid ? "NotYetValid"
                : !isHostnameMatch ? "HostnameMismatch"
                : "UntrustedChain";

            var report = new SslCertificateReport(
                target,
                cert.Subject ?? "",
                cert.Issuer ?? "",
                ExtractCommonName(cert.Subject),
                cert.NotBefore,
                cert.NotAfter,
                (int)(cert.NotAfter - now).TotalDays,
                cert.SerialNumber ?? "",
                cert.SignatureAlgorithm?.FriendlyName ?? "",
                sslStream.SslProtocol.ToString(),
                isExpired,
                isNotYetValid,
                isHostnameMatch,
                isChainTrusted,
                isValid,
                status,
                chainIssues,
                stopwatch.Elapsed);

            return ToolResult<SslCertificateReport>.Ok(report);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return ToolResult<SslCertificateReport>.Fail(
                ToolErrorKind.Unexpected,
                SslCheckerErrorCodes.Timeout,
                $"Connection timed out after {QueryTimeoutMsLimit} ms.");
        }
        catch (AuthenticationException ex)
        {
            stopwatch.Stop();
            return ToolResult<SslCertificateReport>.Fail(
                ToolErrorKind.Unexpected,
                SslCheckerErrorCodes.ConnectionFailed,
                $"TLS handshake failed: {ex.Message}");
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();
            return ToolResult<SslCertificateReport>.Fail(
                ToolErrorKind.NotFound,
                SslCheckerErrorCodes.ConnectionFailed,
                ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<SslCertificateReport>.Fail(
                ToolErrorKind.Unexpected,
                SslCheckerErrorCodes.Unexpected,
                ex.ToString());
        }
    }

    private static bool ValidateChain(X509Certificate2 cert, out IReadOnlyList<string> issues)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        if (chain.Build(cert))
        {
            issues = Array.Empty<string>();
            return true;
        }

        issues = chain.ChainStatus
            .Select(status => string.IsNullOrWhiteSpace(status.StatusInformation)
                ? status.Status.ToString()
                : $"{status.Status}: {status.StatusInformation.Trim()}")
            .ToList();

        return false;
    }

    private static string ExtractCommonName(string? subject)
    {
        if (string.IsNullOrEmpty(subject)) return "";

        foreach (string part in subject.Split(','))
        {
            string trimmed = part.Trim();
            if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                return trimmed[3..];
        }

        return "";
    }

    private static ToolResult<SslCertificateReport> Cancelled() =>
        ToolResult<SslCertificateReport>.Fail(
            ToolErrorKind.Cancelled,
            SslCheckerErrorCodes.Cancelled,
            "SSL check cancelled by the user.");
}