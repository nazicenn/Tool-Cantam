// ViewModels/UuidGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class UuidGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count = 1;

    [ObservableProperty]
    private bool _uppercase = true;

    [ObservableProperty]
    private bool _includeBraces = false;

    [ObservableProperty]
    private bool _includeHyphens = true;

    [ObservableProperty]
    private string _output = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Generate()
    {
        try
        {
            var sb = new StringBuilder();

            for (int i = 0; i < Count; i++)
            {
                var uuid = Guid.NewGuid().ToString();

                if (!IncludeHyphens)
                    uuid = uuid.Replace("-", "");

                if (Uppercase)
                    uuid = uuid.ToUpper();
                else
                    uuid = uuid.ToLower();

                if (IncludeBraces)
                    uuid = "{" + uuid + "}";

                sb.AppendLine(uuid);
            }

            Output = sb.ToString().TrimEnd();
            StatusText = $"{Count} UUID(s) generated";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Output = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(Output))
        {
            Clipboard.SetText(Output);
            StatusText = "Copied to clipboard!";
        }
    }
}