// ViewModels/MarkdownViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig;
using System;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class MarkdownViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputMarkdown = "# Hello World!\n\nThis is **bold** and *italic* text.\n\n- List item 1\n- List item 2\n\n```csharp\nConsole.WriteLine(\"Hello\");\n```";

    [ObservableProperty]
    private string _outputHtml = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Convert()
    {
        if (string.IsNullOrWhiteSpace(InputMarkdown))
        {
            StatusText = "Please enter Markdown to convert";
            OutputHtml = string.Empty;
            return;
        }

        try
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(InputMarkdown, pipeline);

            OutputHtml = $@"<html><head><style>
                body {{ font-family: 'Segoe UI', sans-serif; padding: 20px; line-height: 1.6; }}
                h1, h2, h3 {{ color: #2ecc71; }}
                code {{ background: #2d2d2d; padding: 2px 6px; border-radius: 4px; font-family: 'Consolas', monospace; }}
                pre {{ background: #2d2d2d; padding: 15px; border-radius: 8px; overflow-x: auto; }}
                blockquote {{ border-left: 3px solid #2ecc71; margin: 10px 0; padding-left: 15px; color: #888; }}
                table {{ border-collapse: collapse; width: 100%; }}
                th, td {{ border: 1px solid #444; padding: 8px; text-align: left; }}
                th {{ background: #2d2d2d; }}
                a {{ color: #2ecc71; }}
                </style></head><body>{html}</body></html>";

            StatusText = "✓ Converted Markdown to HTML";
        }
        catch (Exception ex)
        {
            OutputHtml = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputMarkdown = @"# Markdown Örneği

## Başlık 2

Bu bir **kalın** ve *italik* metindir.

### Liste:

- Madde 1
- Madde 2
- Madde 3

### Kod:

```csharp
Console.WriteLine(""Hello World!"");Tablo:
İsim	Yaş
Ahmet	25
Mehmet	30
Alıntı: Bu bir alıntıdır.

Google Link";

        Convert();
        StatusText = "Sample loaded";
    }

    [RelayCommand]
    private void Clear()
    {
        InputMarkdown = string.Empty;
        OutputHtml = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyHtml()
    {
        if (!string.IsNullOrWhiteSpace(OutputHtml))
        {
            Clipboard.SetText(OutputHtml);
            StatusText = "HTML copied to clipboard!";
        }
    }
}