using System;
using System.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class AsciiConverterService : IAsciiConverterService
{
    public string ConvertToAscii(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var asciiCodes = new StringBuilder();
        foreach (char c in text)
        {
            asciiCodes.Append((int)c).Append(' ');
        }

        return asciiCodes.ToString().Trim();
    }

    public string ConvertFromAscii(string asciiCodes)
    {
        if (string.IsNullOrWhiteSpace(asciiCodes))
            return string.Empty;

        var parts = asciiCodes.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var text = new StringBuilder();

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out int asciiCode))
                throw new FormatException($"Invalid ASCII code: {part}");

            if (asciiCode < 0 || asciiCode > 65535)
                throw new ArgumentOutOfRangeException($"ASCII code out of range: {asciiCode}");

            text.Append((char)asciiCode);
        }

        return text.ToString();
    }

    public bool TryParseAsciiCodes(string input, out string result, out string errorMessage)
    {
        result = string.Empty;
        errorMessage = string.Empty;

        try
        {
            result = ConvertFromAscii(input);
            return true;
        }
        catch (FormatException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}