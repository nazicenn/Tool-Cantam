// ViewModels/JwtGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class JwtGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _secretKey = "my-super-secret-key-that-is-at-least-32-chars-long";

    [ObservableProperty]
    private string _issuer = "myApp";

    [ObservableProperty]
    private string _audience = "myUsers";

    [ObservableProperty]
    private int _expiryMinutes = 60;

    [ObservableProperty]
    private string _claims = "userId:123\nusername:admin\nrole:Admin";

    [ObservableProperty]
    private string _generatedToken = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            StatusText = "Please enter a secret key";
            return;
        }

        try
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimsList = new List<Claim>();
            if (!string.IsNullOrWhiteSpace(Claims))
            {
                var lines = Claims.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(':');
                    if (parts.Length == 2)
                    {
                        claimsList.Add(new Claim(parts[0], parts[1]));
                    }
                }
            }

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claimsList,
                expires: DateTime.UtcNow.AddMinutes(ExpiryMinutes),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            GeneratedToken = tokenHandler.WriteToken(token);
            StatusText = "✓ JWT generated successfully";
        }
        catch (Exception ex)
        {
            GeneratedToken = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyToken()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedToken))
        {
            Clipboard.SetText(GeneratedToken);
            StatusText = "Token copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        GeneratedToken = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void LoadSample()
    {
        SecretKey = "my-super-secret-key-that-is-at-least-32-chars-long";
        Issuer = "myApp";
        Audience = "myUsers";
        ExpiryMinutes = 60;
        Claims = "userId:123\nusername:admin\nrole:Admin";
        StatusText = "Sample loaded";
    }

    [RelayCommand]
    private void GenerateStrongKey()
    {
        SecretKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                    Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        SecretKey = SecretKey.Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 32);
        StatusText = "✓ Strong key generated (32 chars)";
    }
}