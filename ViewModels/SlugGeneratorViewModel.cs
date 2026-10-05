// ViewModels/SlugGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace tool_cantam.ViewModels;

public partial class SlugGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private bool _toLowercase = true;

    [ObservableProperty]
    private string _separator = "-";

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            return;
        }

        try
        {
            var slug = InputText.Trim();

            // Türkçe karakter dönüşümü
            var replacements = new (string from, string to)[]
            {
                ("ğ", "g"), ("Ğ", "g"),
                ("ü", "u"), ("Ü", "u"),
                ("ş", "s"), ("Ş", "s"),
                ("ı", "i"), ("I", "i"),
                ("ö", "o"), ("Ö", "o"),
                ("ç", "c"), ("Ç", "c"),
                (" ", Separator),
            };

            foreach (var rep in replacements)
            {
                slug = slug.Replace(rep.from, rep.to);
            }

            // Özel karakterleri temizle
            slug = Regex.Replace(slug, @"[^a-zA-Z0-9\-]", "");

            // Birden fazla separator'ı tek yap
            slug = Regex.Replace(slug, $@"{Regex.Escape(Separator)}+", Separator);

            // Baştaki ve sondaki separator'ları temizle
            slug = slug.Trim(Separator[0]);

            if (ToLowercase)
                slug = slug.ToLower();

            OutputText = slug;
            StatusText = "Slug generated successfully!";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputText))
        {
            Clipboard.SetText(OutputText);
            StatusText = "Copied to clipboard!";
        }
    }
}