using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class MetadataViewModel : ObservableObject
{
    private readonly IMetadataService _metadataService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public MetadataViewModel(IMetadataService metadataService, IFileExportService fileExportService)
    {
        _metadataService = metadataService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void SelectFile()
    {
        var selectedPath = _fileExportService.RequestOpenPath(
            "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif)|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|All Files (*.*)|*.*",
            "Select Image File");
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            FilePath = selectedPath;
            StatusMessage = "File selected.";
        }
    }

    [RelayCommand]
    private async Task RemoveMetadataAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            StatusMessage = "Please select a file first.";
            return;
        }

        var filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|All Files|*.*";
        var destPath = _fileExportService.RequestSavePath(filter, Path.GetExtension(FilePath), $"Cleaned_{Path.GetFileName(FilePath)}");

        if (string.IsNullOrEmpty(destPath))
            return;

        try
        {
            StatusMessage = "Purging metadata and saving...";
            bool success = await _metadataService.PurgeExifAsync(FilePath, destPath);
            StatusMessage = success ? "Metadata purged successfully. Clean file saved." : "Operation failed.";
        }
        catch (NotSupportedException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (FileNotFoundException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}