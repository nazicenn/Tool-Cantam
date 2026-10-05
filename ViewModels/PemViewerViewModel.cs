// ViewModels/PemViewerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class PemViewerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pemInput = string.Empty;

    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _issuer = string.Empty;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _validFrom = string.Empty;

    [ObservableProperty]
    private string _validTo = string.Empty;

    [ObservableProperty]
    private string _publicKey = string.Empty;

    [ObservableProperty]
    private string _signatureAlgorithm = string.Empty;

    [ObservableProperty]
    private string _thumbprint = string.Empty;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Parse()
    {
        if (string.IsNullOrWhiteSpace(PemInput))
        {
            StatusText = "Please enter PEM certificate";
            return;
        }

        try
        {
            var cert = ParsePem(PemInput);
            if (cert == null)
            {
                StatusText = "Invalid PEM certificate";
                return;
            }

            Subject = cert.Subject ?? "N/A";
            Issuer = cert.Issuer ?? "N/A";
            SerialNumber = cert.SerialNumber ?? "N/A";
            ValidFrom = cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss");
            ValidTo = cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss");
            PublicKey = cert.PublicKey?.Oid?.FriendlyName ?? "N/A";
            SignatureAlgorithm = cert.SignatureAlgorithm?.FriendlyName ?? "N/A";
            Thumbprint = cert.Thumbprint ?? "N/A";
            IsValid = DateTime.Now >= cert.NotBefore && DateTime.Now <= cert.NotAfter;

            StatusText = IsValid ? "✓ Valid certificate" : "⚠️ Expired certificate";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            ClearResults();
        }
    }

    private X509Certificate2? ParsePem(string pem)
    {
        var match = Regex.Match(pem, @"-----BEGIN CERTIFICATE-----(.*?)-----END CERTIFICATE-----", RegexOptions.Singleline);
        if (match.Success)
        {
            var base64 = match.Groups[1].Value.Trim().Replace("\n", "").Replace("\r", "");
            var bytes = Convert.FromBase64String(base64);

            // UYARI YOK - Yeni API
            var cert = X509CertificateLoader.LoadCertificate(bytes);
            return cert;
        }
        return null;
    }

    [RelayCommand]
    private void LoadSample()
    {
        PemInput = @"-----BEGIN CERTIFICATE-----
MIIDdTCCAl2gAwIBAgIJAMkq5B2D7NQDMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV
BAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMRswGQYDVQQDExJHcmFu
dCBUZXN0IEVWIFJvb3QwHhcNMjQwMTAxMDAwMDAwWhcNMjUwMTAxMDAwMDAwWjBF
MQswCQYDVQQGEwJCRTEZMBcGA1UEChMQR2xvYmFsU2lnbiBudi1zYTEbMBkGA1UE
AxMSR3JhbnQgVGVzdCBFViBSb290MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIB
CgKCAQEAym9i1d0bK4h9RNCZ6eM3jFk5M8VnZjBdJkB5LfPv2tZyL8shQl5QjK9K
lQNkQxkJjB5vJqXV9GkqWKZqZqYjyUqN8BzQp1bM8Xz5qVwJqKqVn8jLqLqL9aXQ
GZu7dZ9qVp9rLpVn8pVz9qVp9rLpVn8pVz9qVp9rLpVn8pVz9qVp9rLpVn8pVz9q
AQIDAQABo4G+MIG7MB0GA1UdDgQWBBQjL9qLqL9aXQZu7dZ9qVp9rLpVn8pVzA9
BgNVHSMENjA0gBQjL9qLqL9aXQZu7dZ9qVp9rLpVn8pVzKEpMCcwJQYDVR0RBB4w
HIIUZ3JhbnQudGVzdC1ldi5yb290LmNvbTALBgNVHQ8EBAMCAqQwHQYDVR0lBBYw
FAYIKwYBBQUHAwEGCCsGAQUFBwMCMA0GCSqGSIb3DQEBCwUAA4IBAQCMq7qLqL9a
XQZu7dZ9qVp9rLpVn8pVz9qVp9rLpVn8pVz9qVp9rLpVn8pVz9qVp9rLpVn8pVz9
-----END CERTIFICATE-----";
        Parse();
    }

    [RelayCommand]
    private void Clear()
    {
        PemInput = string.Empty;
        ClearResults();
        StatusText = "Cleared";
    }

    private void ClearResults()
    {
        Subject = string.Empty;
        Issuer = string.Empty;
        SerialNumber = string.Empty;
        ValidFrom = string.Empty;
        ValidTo = string.Empty;
        PublicKey = string.Empty;
        SignatureAlgorithm = string.Empty;
        Thumbprint = string.Empty;
        IsValid = false;
    }
}