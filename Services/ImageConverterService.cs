using System;
using System.IO;
using System.Linq;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class ImageConverterService : IImageConverterService
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tiff" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public string ConvertImageToBase64(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Dosya yolu boş olamaz", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

        // Güvenlik: Sadece resim dosyaları
        if (!IsValidImageFile(filePath))
            throw new InvalidOperationException($"Güvenlik: Sadece resim dosyaları desteklenir! İzin verilen formatlar: {string.Join(", ", AllowedExtensions)}");

        byte[] imageBytes = File.ReadAllBytes(filePath);

        // Güvenlik: Dosya boyutu kontrolü
        if (imageBytes.Length > MaxFileSize)
            throw new InvalidOperationException($"Dosya çok büyük! Maksimum {MaxFileSize / 1024 / 1024} MB");

        // Güvenlik: İçerik doğrulama
        if (!IsValidImageBytes(imageBytes))
            throw new InvalidOperationException("Dosya geçerli bir resim formatında değil");

        string mimeType = GetMimeTypeFromFilePath(filePath);
        return ConvertImageToBase64(imageBytes, mimeType);
    }

    public string ConvertImageToBase64(byte[] imageBytes, string mimeType)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Resim verisi boş olamaz", nameof(imageBytes));

        string base64 = Convert.ToBase64String(imageBytes);
        return $"data:{mimeType};base64,{base64}";
    }

    public byte[] ConvertBase64ToImageBytes(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            throw new ArgumentException("Base64 metni boş olamaz", nameof(base64String));

        string pureBase64 = base64String;
        if (base64String.Contains(","))
            pureBase64 = base64String.Substring(base64String.IndexOf(",") + 1);

        byte[] imageBytes = Convert.FromBase64String(pureBase64);

        // Güvenlik: Çıktı da resim mi kontrol et
        if (!IsValidImageBytes(imageBytes))
            throw new InvalidOperationException("Bu Base64 verisi geçerli bir resim formatında değil");

        return imageBytes;
    }

    public bool TryConvertBase64ToImage(string base64String, out byte[] imageBytes, out string errorMessage)
    {
        imageBytes = Array.Empty<byte>();
        errorMessage = string.Empty;

        try
        {
            imageBytes = ConvertBase64ToImageBytes(base64String);
            return true;
        }
        catch (FormatException ex)
        {
            errorMessage = "Geçersiz Base64 formatı: " + ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Dönüşüm başarısız: {ex.Message}";
            return false;
        }
    }

    public string GetMimeTypeFromFilePath(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".tiff" => "image/tiff",
            _ => "image/octet-stream"
        };
    }

    public string GetMimeTypeFromBase64(string base64String)
    {
        if (!string.IsNullOrWhiteSpace(base64String) && base64String.Contains("data:"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(base64String, @"data:([^;]+);");
            if (match.Success)
                return match.Groups[1].Value;
        }
        return "image/png";
    }

    public bool IsValidImageFile(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return AllowedExtensions.Contains(ext);
    }

    public bool IsValidImageBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
            return false;

        // PNG: 89 50 4E 47
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return true;

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;

        // GIF: 47 49 46 38
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
            return true;

        // BMP: 42 4D
        if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            return true;

        // WEBP: 52 49 46 46
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
            return true;

        return false;
    }
}