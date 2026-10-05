using System;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class UrlService : IUrlService
{
    public string Encode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        try
        {
            return Uri.EscapeDataString(text);
        }
        catch
        {
            return string.Empty;
        }
    }

    public string Decode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        try
        {
            return Uri.UnescapeDataString(text);
        }
        catch
        {
            return string.Empty;
        }
    }
}