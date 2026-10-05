using System;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class ClipboardService : IClipboardService
{
    public void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy to clipboard: {ex.Message}", ex);
        }
    }

    public async Task<string?> GetFromClipboardAsync()
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch
            {
                return null;
            }
        });
    }

    public bool ContainsText()
    {
        try
        {
            return Clipboard.ContainsText();
        }
        catch
        {
            return false;
        }
    }
}