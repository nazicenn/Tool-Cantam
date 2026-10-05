namespace tool_cantam.Services.Interfaces;

public interface IAsciiConverterService
{
    string ConvertToAscii(string text);
    string ConvertFromAscii(string asciiCodes);
    bool TryParseAsciiCodes(string input, out string result, out string errorMessage);
}