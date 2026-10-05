using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class SslCheckerService : ISslCheckerService
{
    public bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return false;
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }

    public async Task<SslCertificateInfo> CheckAsync(string domain)
    {
        var result = new SslCertificateInfo { Domain = domain };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(domain, 443);

            // El sıkışma sırasında hata yok sayılır; ama sertifika bilgisi okunabilsin diye
            // geri çağrı true döner. Gerçek sonuç aşağıda policyErrors + zincirden hesaplanır.
            var policyErrors = SslPolicyErrors.None;
            using var sslStream = new SslStream(client.GetStream(), false, (sender, certificate, chain, errors) =>
            {
                policyErrors = errors;
                return true;
            });

            await sslStream.AuthenticateAsClientAsync(domain);

            if (sslStream.RemoteCertificate is not X509Certificate2 cert)
            {
                result.Status = "No certificate found";
                result.IsValid = false;
                return result;
            }

            result.Subject = cert.Subject ?? "Unknown";
            result.Issuer = cert.Issuer ?? "Unknown";
            result.NotBefore = cert.NotBefore;
            result.NotAfter = cert.NotAfter;
            result.SerialNumber = cert.SerialNumber ?? "Unknown";
            result.SignatureAlgorithm = cert.SignatureAlgorithm?.FriendlyName ?? "Unknown";
            result.RemainingDays = (int)(cert.NotAfter - DateTime.Now).TotalDays;

            var now = DateTime.Now;
            var isExpired = now > cert.NotAfter;
            var isNotYetValid = now < cert.NotBefore;

            var isChainValid = ValidateChain(cert, out var chainErrorText);
            var isHostNameMismatch = (policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0;
            var isChainInvalid = (policyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 || !isChainValid;

            result.IsExpired = isExpired;
            result.IsValid = !isExpired && !isNotYetValid && !isHostNameMismatch && !isChainInvalid;

            if (result.IsValid)
            {
                result.Status = "Valid";
                result.ErrorMessage = string.Empty;
            }
            else
            {
                if (isExpired) result.Status = "Expired";
                else if (isNotYetValid) result.Status = "NotYetValid";
                else if (isHostNameMismatch) result.Status = "HostnameMismatch";
                else result.Status = "UntrustedChain";

                var details = new List<string>();
                if (isHostNameMismatch)
                    details.Add("Certificate does not match the requested hostname.");
                if (isChainInvalid)
                    details.Add(string.IsNullOrEmpty(chainErrorText) ? "Certificate chain is not trusted." : chainErrorText);
                if (isExpired)
                    details.Add($"Certificate expired on {cert.NotAfter:u}.");
                if (isNotYetValid)
                    details.Add($"Certificate is not valid before {cert.NotBefore:u}.");

                result.ErrorMessage = string.Join(" ", details);
            }
        }
        catch (Exception ex)
        {
            result.Status = "Error";
            result.ErrorMessage = ex.Message ?? "Unknown error";
            result.IsValid = false;
        }

        return result;
    }

    private static bool ValidateChain(X509Certificate2 cert, out string errorText)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        if (chain.Build(cert))
        {
            errorText = string.Empty;
            return true;
        }

        errorText = string.Join(" ", chain.ChainStatus.Select(status =>
            string.IsNullOrWhiteSpace(status.StatusInformation)
                ? status.Status.ToString()
                : $"{status.Status}: {status.StatusInformation.Trim()}"));

        return false;
    }
}