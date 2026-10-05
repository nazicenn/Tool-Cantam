namespace tool_cantam.Services.Interfaces;

public interface IClipboardService
{
    void CopyToClipboard(string text);
    Task<string?> GetFromClipboardAsync();
    bool ContainsText();
}