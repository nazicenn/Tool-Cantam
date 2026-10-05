using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class UnixTimestampViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _timestampInput = string.Empty;

    [ObservableProperty]
    private string _dateOutput = string.Empty;

    [ObservableProperty]
    private string _dateInput = string.Empty;

    [ObservableProperty]
    private string _timestampOutput = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private string _currentTimestamp = string.Empty;

    [ObservableProperty]
    private bool _useUtc = false;

    public UnixTimestampViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
        UpdateCurrentTimestamp();
    }

    private void UpdateCurrentTimestamp()
    {
        CurrentTimestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
    }

    [RelayCommand]
    private void TimestampToDate()
    {
        if (string.IsNullOrWhiteSpace(TimestampInput))
        {
            StatusText = "Please enter a Unix timestamp";
            DateOutput = string.Empty;
            return;
        }

        try
        {
            if (long.TryParse(TimestampInput.Trim(), out long seconds))
            {
                var dateTime = DateTimeOffset.FromUnixTimeSeconds(seconds);
                DateOutput = UseUtc
                    ? dateTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                    : dateTime.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                StatusText = "Converted timestamp to date";
            }
            else
            {
                StatusText = "Invalid timestamp format";
                DateOutput = string.Empty;
            }
        }
        catch (Exception ex)
        {
            DateOutput = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DateToTimestamp()
    {
        if (string.IsNullOrWhiteSpace(DateInput))
        {
            StatusText = "Please enter a date (yyyy-MM-dd HH:mm:ss)";
            TimestampOutput = string.Empty;
            return;
        }

        try
        {
            if (DateTime.TryParse(DateInput.Trim(), out DateTime date))
            {
                DateTime utcDate = UseUtc ? date : date.ToUniversalTime();
                var timestamp = new DateTimeOffset(utcDate).ToUnixTimeSeconds();
                TimestampOutput = timestamp.ToString();
                StatusText = "Converted date to timestamp";
            }
            else
            {
                StatusText = "Invalid date format. Use: yyyy-MM-dd HH:mm:ss";
                TimestampOutput = string.Empty;
            }
        }
        catch (Exception ex)
        {
            TimestampOutput = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyDateResult()
    {
        if (!string.IsNullOrWhiteSpace(DateOutput))
        {
            _fileExportService.CopyToClipboard(DateOutput);
            StatusText = "Date copied to clipboard";
        }
    }

    [RelayCommand]
    private void CopyTimestampResult()
    {
        if (!string.IsNullOrWhiteSpace(TimestampOutput))
        {
            _fileExportService.CopyToClipboard(TimestampOutput);
            StatusText = "Timestamp copied to clipboard";
        }
    }

    [RelayCommand]
    private void UseCurrentTimestamp()
    {
        UpdateCurrentTimestamp();
        TimestampInput = CurrentTimestamp;
        TimestampToDate();
        StatusText = "Current timestamp loaded";
    }

    partial void OnUseUtcChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(TimestampInput))
            TimestampToDate();
        if (!string.IsNullOrWhiteSpace(DateInput))
            DateToTimestamp();
        StatusText = value ? "Switched to UTC mode" : "Switched to Local mode";
    }

    [RelayCommand]
    private void Clear()
    {
        TimestampInput = string.Empty;
        DateOutput = string.Empty;
        DateInput = string.Empty;
        TimestampOutput = string.Empty;
        UpdateCurrentTimestamp();
        StatusText = "Cleared. Ready.";
    }
}