// ViewModels/RandomStringGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class RandomStringGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 12;

    [ObservableProperty]
    private int _count = 1;

    [ObservableProperty]
    private bool _includeUppercase = true;

    [ObservableProperty]
    private bool _includeLowercase = true;

    [ObservableProperty]
    private bool _includeNumbers = true;

    [ObservableProperty]
    private bool _includeSpecial = false;

    [ObservableProperty]
    private string _output = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Numbers = "0123456789";
    private const string Special = "!@#$%^&*()_+-=[]{};:,.<>?";

    [RelayCommand]
    private void Generate()
    {
        try
        {
            var charPool = new StringBuilder();
            if (IncludeUppercase) charPool.Append(Uppercase);
            if (IncludeLowercase) charPool.Append(Lowercase);
            if (IncludeNumbers) charPool.Append(Numbers);
            if (IncludeSpecial) charPool.Append(Special);

            if (charPool.Length == 0)
            {
                StatusText = "Select at least one character type";
                return;
            }

            var pool = charPool.ToString();
            var random = new Random();
            var results = new StringBuilder();

            for (int i = 0; i < Count; i++)
            {
                var result = new char[Length];
                for (int j = 0; j < Length; j++)
                {
                    result[j] = pool[random.Next(pool.Length)];
                }
                results.AppendLine(new string(result));
            }

            Output = results.ToString().TrimEnd();
            StatusText = $"{Count} random string(s) generated";
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