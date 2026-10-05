using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Helpers;
using tool_cantam.Services;

namespace tool_cantam.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private string _selectedLanguage = "en-US";

    public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;

        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var settings = _settingsService.Load();


            SelectedLanguage = settings.Language ?? "en-US";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading settings.");
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        LocalizationService.SetLanguage(value);
        Save();
    }

    [RelayCommand]
    private void SetLanguage(string lang) => SelectedLanguage = lang;

    private void Save()
    {
        var settings = new AppSettings { Language = SelectedLanguage };
        _settingsService.Save(settings);
    }
}