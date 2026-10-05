using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class HashIdentifierViewModel : ObservableObject
{
    private readonly IHashIdentifierService _hashIdentifierService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _hashInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _detectedTypes = new();

    [ObservableProperty]
    private string _statusText = "Ready.";

    public HashIdentifierViewModel(IHashIdentifierService hashIdentifierService, IFileExportService fileExportService)
    {
        _hashIdentifierService = hashIdentifierService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void Identify()
    {
        if (string.IsNullOrWhiteSpace(HashInput))
        {
            StatusText = "Please enter a hash to identify";
            DetectedTypes.Clear();
            return;
        }

        var results = _hashIdentifierService.Identify(HashInput);
        DetectedTypes.Clear();
        foreach (var result in results)
            DetectedTypes.Add(result);

        StatusText = results.FirstOrDefault() == "Unknown / Unsupported hash format"
            ? "Could not identify the hash type"
            : "Hash identified successfully";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (DetectedTypes.Count == 0) return;

        var results = string.Join(", ", DetectedTypes);
        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        HashInput = string.Empty;
        DetectedTypes.Clear();
        StatusText = "Cleared. Ready.";
    }
}