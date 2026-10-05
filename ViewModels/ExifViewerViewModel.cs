// ViewModels/ExifViewerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetadataExtractor;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class ExifViewerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _imagePreview = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ExifDirectoryInfo> Directories { get; } = new();

    public class ExifDirectoryInfo
    {
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<ExifTagInfo> Tags { get; set; } = new();
    }

    public class ExifTagInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    [RelayCommand]
    private void LoadImage()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            StatusText = "Please select or drop an image file";
            return;
        }

        try
        {
            if (!File.Exists(FilePath))
            {
                StatusText = "File not found";
                return;
            }

            ImagePreview = FilePath;

            var directories = ImageMetadataReader.ReadMetadata(FilePath);

            Directories.Clear();

            if (directories != null)
            {
                foreach (var directory in directories)
                {
                    if (directory == null) continue;

                    var dirInfo = new ExifDirectoryInfo { Name = directory.Name ?? "Unknown" };

                    if (directory.Tags != null)
                    {
                        foreach (var tag in directory.Tags)
                        {
                            if (tag == null) continue;

                            string value = tag.Description ?? tag.Name ?? "N/A";

                            dirInfo.Tags.Add(new ExifTagInfo
                            {
                                Name = tag.Name ?? "Unknown",
                                Value = value
                            });
                        }
                    }

                    if (dirInfo.Tags.Count > 0)
                    {
                        Directories.Add(dirInfo);
                    }
                }
            }

            StatusText = $"✓ Loaded {Directories.Count} directories from {Path.GetFileName(FilePath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            Directories.Clear();
        }
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (Directories.Count == 0)
        {
            StatusText = "No EXIF data to copy";
            return;
        }

        var sb = new StringBuilder();
        foreach (var dir in Directories)
        {
            if (dir == null) continue;
            sb.AppendLine($"[{dir.Name}]");
            if (dir.Tags != null)
            {
                foreach (var tag in dir.Tags)
                {
                    if (tag == null) continue;
                    sb.AppendLine($"  {tag.Name}: {tag.Value}");
                }
            }
            sb.AppendLine();
        }

        Clipboard.SetText(sb.ToString());
        StatusText = "EXIF data copied to clipboard!";
    }

    [RelayCommand]
    private void Clear()
    {
        FilePath = string.Empty;
        ImagePreview = string.Empty;
        Directories.Clear();
        StatusText = "Cleared";
    }
}