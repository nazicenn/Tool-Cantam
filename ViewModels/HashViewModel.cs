using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class HashViewModel : ObservableObject
{
    private readonly IHashService _hashService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _selectedAlgorithm = "SHA256";
    [ObservableProperty] private string _hashOutput = string.Empty;
    [ObservableProperty] private string _compareHash = string.Empty;
    [ObservableProperty] private string _validationResult = string.Empty;

    public string[] Algorithms { get; } = { "MD5", "SHA1", "SHA256", "SHA384", "SHA512" };

    public HashViewModel(IHashService hashService, IFileExportService fileExportService)
    {
        _hashService = hashService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void ComputeText()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            MessageBox.Show("Please enter text to compute hash.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            HashOutput = _hashService.ComputeFromText(InputText, SelectedAlgorithm);
            ValidateHash();
        }
        catch (Exception ex)
        {
            HashOutput = string.Empty;
            ValidationResult = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (!string.IsNullOrWhiteSpace(HashOutput))
        {
            _fileExportService.CopyToClipboard(HashOutput);
            ValidationResult = "Hash copied to clipboard.";
        }
    }

    [RelayCommand]
    private async Task ComputeFileAsync()
    {
        string sourceFile = FilePath;
        if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
        {
            var selectedPath = _fileExportService.RequestOpenPath("All Files (*.*)|*.*", "Select File for Hashing");
            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            sourceFile = selectedPath;
            FilePath = sourceFile;
        }

        try
        {
            HashOutput = await _hashService.ComputeFromFileAsync(sourceFile, SelectedAlgorithm);
            ValidateHash();
        }
        catch (Exception ex)
        {
            HashOutput = string.Empty;
            ValidationResult = $"Error: {ex.Message}";
        }
    }

    partial void OnCompareHashChanged(string value) => ValidateHash();

    private void ValidateHash()
    {
        if (string.IsNullOrWhiteSpace(CompareHash) || string.IsNullOrWhiteSpace(HashOutput))
        {
            ValidationResult = string.Empty;
            return;
        }

        ValidationResult = string.Equals(HashOutput, CompareHash.Trim(), StringComparison.OrdinalIgnoreCase)
            ? "Hash Match Verified"
            : "Hash Mismatch";
    }
}