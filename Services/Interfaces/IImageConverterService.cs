using System;

namespace tool_cantam.Services.Interfaces;

public interface IImageConverterService
{
    string ConvertImageToBase64(string filePath);
    string ConvertImageToBase64(byte[] imageBytes, string mimeType);
    byte[] ConvertBase64ToImageBytes(string base64String);
    bool TryConvertBase64ToImage(string base64String, out byte[] imageBytes, out string errorMessage);
    string GetMimeTypeFromFilePath(string filePath);
    string GetMimeTypeFromBase64(string base64String);
    bool IsValidImageFile(string filePath);
    bool IsValidImageBytes(byte[] imageBytes);
}