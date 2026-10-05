// ViewModels/CssGradientGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Media;

namespace tool_cantam.ViewModels;

public partial class CssGradientGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _color1 = "#FF5733";

    [ObservableProperty]
    private string _color2 = "#33FF57";

    [ObservableProperty]
    private string _direction = "to right";

    [ObservableProperty]
    private string _gradientCss = string.Empty;

    [ObservableProperty]
    private string _previewColor = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Generate()
    {
        try
        {
            GradientCss = $"background: linear-gradient({Direction}, {Color1}, {Color2});";
            PreviewColor = GradientCss;
            StatusText = "✓ CSS gradient generated";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyCss()
    {
        if (!string.IsNullOrWhiteSpace(GradientCss))
        {
            Clipboard.SetText(GradientCss);
            StatusText = "CSS copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Color1 = "#FF5733";
        Color2 = "#33FF57";
        Direction = "to right";
        GradientCss = string.Empty;
        PreviewColor = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void LoadSample()
    {
        Color1 = "#FF5733";
        Color2 = "#33FF57";
        Direction = "to right";
        Generate();
    }
}