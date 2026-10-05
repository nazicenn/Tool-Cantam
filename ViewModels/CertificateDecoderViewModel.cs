// ViewModels/CertificateDecoderViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class CertificateDecoderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _certificateInput = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _signatureAlgorithm = string.Empty;

    [ObservableProperty]
    private string _issuer = string.Empty;

    [ObservableProperty]
    private string _validFrom = string.Empty;

    [ObservableProperty]
    private string _validTo = string.Empty;

    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _publicKeyAlgorithm = string.Empty;

    [ObservableProperty]
    private string _publicKeySize = string.Empty;

    [ObservableProperty]
    private string _thumbprint = string.Empty;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ExtensionInfo> Extensions { get; } = new();

    public class ExtensionInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    [RelayCommand]
    private void Decode()
    {
        if (string.IsNullOrWhiteSpace(CertificateInput))
        {
            StatusText = "Please enter certificate (PEM or Base64)";
            return;
        }

        try
        {
            var cert = ParseCertificate(CertificateInput);
            if (cert == null)
            {
                StatusText = "Invalid certificate format";
                return;
            }

            Version = $"V{cert.Version}";
            SerialNumber = cert.SerialNumber ?? "N/A";
            SignatureAlgorithm = cert.SignatureAlgorithm?.FriendlyName ?? "N/A";
            Issuer = cert.Issuer ?? "N/A";
            ValidFrom = cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss");
            ValidTo = cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss");
            Subject = cert.Subject ?? "N/A";
            PublicKeyAlgorithm = cert.PublicKey?.Oid?.FriendlyName ?? "N/A";
            PublicKeySize = GetPublicKeySize(cert).ToString();
            Thumbprint = cert.Thumbprint ?? "N/A";
            IsValid = DateTime.Now >= cert.NotBefore && DateTime.Now <= cert.NotAfter;

            Extensions.Clear();
            foreach (var ext in cert.Extensions)
            {
                Extensions.Add(new ExtensionInfo
                {
                    Name = ext.Oid?.FriendlyName ?? ext.Oid?.Value ?? "Unknown",
                    Value = ext.Format(true) ?? "N/A"
                });
            }

            StatusText = IsValid ? "✓ Certificate is valid" : "⚠️ Certificate is expired/invalid";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            ClearResults();
        }
    }

    private X509Certificate2? ParseCertificate(string input)
    {
        // Check if it's PEM format
        var pemMatch = Regex.Match(input, @"-----BEGIN CERTIFICATE-----(.*?)-----END CERTIFICATE-----", RegexOptions.Singleline);
        if (pemMatch.Success)
        {
            var base64 = pemMatch.Groups[1].Value.Trim().Replace("\n", "").Replace("\r", "");
            var bytes = Convert.FromBase64String(base64);
            return X509CertificateLoader.LoadCertificate(bytes);
        }

        // Try as raw Base64
        try
        {
            var bytes = Convert.FromBase64String(input.Trim());
            return X509CertificateLoader.LoadCertificate(bytes);
        }
        catch
        {
            // Try as file path
            try
            {
                return X509CertificateLoader.LoadCertificateFromFile(input);
            }
            catch
            {
                return null;
            }
        }
    }

    private int GetPublicKeySize(X509Certificate2 cert)
    {
        try
        {
            using (var rsa = cert.GetRSAPublicKey())
            {
                if (rsa != null) return rsa.KeySize;
            }
            using (var ecdsa = cert.GetECDsaPublicKey())
            {
                if (ecdsa != null) return ecdsa.KeySize;
            }
        }
        catch { }
        return 0;
    }

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.Filter = "Certificate files (*.cer;*.crt;*.pem)|*.cer;*.crt;*.pem|All files (*.*)|*.*";
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var bytes = System.IO.File.ReadAllBytes(dialog.FileName);
                var cert = X509CertificateLoader.LoadCertificate(bytes);
                CertificateInput = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
                Decode();
                StatusText = $"✓ Loaded from: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusText = $"✗ Error loading file: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CertificateInput = string.Empty;
        ClearResults();
        StatusText = "Cleared";
    }

    private void ClearResults()
    {
        Version = string.Empty;
        SerialNumber = string.Empty;
        SignatureAlgorithm = string.Empty;
        Issuer = string.Empty;
        ValidFrom = string.Empty;
        ValidTo = string.Empty;
        Subject = string.Empty;
        PublicKeyAlgorithm = string.Empty;
        PublicKeySize = string.Empty;
        Thumbprint = string.Empty;
        IsValid = false;
        Extensions.Clear();
    }
}