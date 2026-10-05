// ViewModels/XmlFormatterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class XmlFormatterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputXml = string.Empty;

    [ObservableProperty]
    private string _outputXml = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputXml))
        {
            StatusText = "Please enter XML to format";
            OutputXml = string.Empty;
            return;
        }

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(InputXml);

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            OutputXml = sb.ToString();
            StatusText = "✓ XML formatted successfully";
        }
        catch (XmlException ex)
        {
            OutputXml = string.Empty;
            StatusText = $"✗ Invalid XML: {ex.Message}";
        }
        catch (Exception ex)
        {
            OutputXml = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Minify()
    {
        if (string.IsNullOrWhiteSpace(InputXml))
        {
            StatusText = "Please enter XML to minify";
            return;
        }

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(InputXml);

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = false,
                NewLineHandling = NewLineHandling.None
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            OutputXml = sb.ToString();
            StatusText = "✓ XML minified successfully";
        }
        catch (XmlException ex)
        {
            StatusText = $"✗ Invalid XML: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputXml = string.Empty;
        OutputXml = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputXml))
        {
            Clipboard.SetText(OutputXml);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputXml, OutputXml) = (OutputXml, InputXml);
        StatusText = "Swapped";
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputXml = "<?xml version=\"1.0\"?><root><user id=\"1\"><name>Ahmet</name><city>İstanbul</city></user><user id=\"2\"><name>Mehmet</name><city>Ankara</city></user></root>";
        Format();
    }
}