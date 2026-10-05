// ViewModels/RsaKeyGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class RsaKeyGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _publicKey = string.Empty;

    [ObservableProperty]
    private string _privateKey = string.Empty;

    private int _keySize = 2048;
    public int KeySize
    {
        get => _keySize;
        set
        {
            _keySize = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private bool _includeXmlFormat = true;

    [ObservableProperty]
    private string _statusText = "Ready";

    private RSA? _rsa;

    [RelayCommand]
    private void Generate()
    {
        try
        {
            _rsa?.Dispose();
            _rsa = RSA.Create(KeySize);

            if (IncludeXmlFormat)
            {
                PublicKey = ExportToXml(_rsa, false) ?? string.Empty;
                PrivateKey = ExportToXml(_rsa, true) ?? string.Empty;
            }
            else
            {
                var publicKeyBytes = _rsa.ExportSubjectPublicKeyInfo();
                var privateKeyBytes = _rsa.ExportPkcs8PrivateKey();

                PublicKey = publicKeyBytes != null ? Convert.ToBase64String(publicKeyBytes) : string.Empty;
                PrivateKey = privateKeyBytes != null ? Convert.ToBase64String(privateKeyBytes) : string.Empty;
            }

            StatusText = $"✓ RSA key pair generated ({KeySize} bit)";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    private string ExportToXml(RSA rsa, bool includePrivate)
    {
        var params_ = rsa.ExportParameters(includePrivate);
        var sb = new StringBuilder();
        sb.Append("<RSAKeyValue>");

        if (params_.Modulus != null)
            sb.Append($"<Modulus>{Convert.ToBase64String(params_.Modulus)}</Modulus>");
        if (params_.Exponent != null)
            sb.Append($"<Exponent>{Convert.ToBase64String(params_.Exponent)}</Exponent>");

        if (includePrivate)
        {
            if (params_.P != null)
                sb.Append($"<P>{Convert.ToBase64String(params_.P)}</P>");
            if (params_.Q != null)
                sb.Append($"<Q>{Convert.ToBase64String(params_.Q)}</Q>");
            if (params_.DP != null)
                sb.Append($"<DP>{Convert.ToBase64String(params_.DP)}</DP>");
            if (params_.DQ != null)
                sb.Append($"<DQ>{Convert.ToBase64String(params_.DQ)}</DQ>");
            if (params_.InverseQ != null)
                sb.Append($"<InverseQ>{Convert.ToBase64String(params_.InverseQ)}</InverseQ>");
            if (params_.D != null)
                sb.Append($"<D>{Convert.ToBase64String(params_.D)}</D>");
        }

        sb.Append("</RSAKeyValue>");
        return sb.ToString();
    }

    [RelayCommand]
    private void CopyPublicKey()
    {
        if (!string.IsNullOrWhiteSpace(PublicKey))
        {
            Clipboard.SetText(PublicKey);
            StatusText = "Public key copied to clipboard!";
        }
    }

    [RelayCommand]
    private void CopyPrivateKey()
    {
        if (!string.IsNullOrWhiteSpace(PrivateKey))
        {
            Clipboard.SetText(PrivateKey);
            StatusText = "Private key copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        PublicKey = string.Empty;
        PrivateKey = string.Empty;
        _rsa?.Dispose();
        _rsa = null;
        StatusText = "Cleared";
    }
}