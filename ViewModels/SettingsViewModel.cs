using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;

namespace tool_cantam.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private string _selectedLanguage = "en-US";

    [ObservableProperty] private string _statusText = string.Empty;

    public SettingsViewModel(
        ISettingsService settingsService,
        ILocalizerService localizer,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _localizer = localizer;
        _logger = logger;

        LoadSettings();
    }

    /// <summary>Arayüzde liste olarak gösterilecek desteklenen diller.</summary>
    public IReadOnlyCollection<string> AvailableLanguages => _localizer.AvailableCultures;

    private void LoadSettings()
    {
        try
        {
            var settings = _settingsService.Load();

            // Eski ayar dosyalarındaki "English"/"Türkçe" gibi değerler normalize edilir.
            SelectedLanguage = Helpers.LocalizationService.TryNormalizeCulture(settings.Language, out string culture)
                ? culture
                : Helpers.LocalizationService.DefaultCultureCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings could not be loaded.");
            StatusText = _localizer.Get("L.Common.Error.SettingsLoad");
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // Dil değişimi başarısız olursa kullanıcı bunu görmeli; sessizce yutulmaz.
        if (!_localizer.SetCulture(value))
        {
            _logger.LogWarning("Language could not be changed to {Culture}: {Error}", value, Helpers.LocalizationService.LastError);
            StatusText = _localizer.Get("L.Common.Error.LanguageChange");
            return;
        }

        StatusText = _localizer.Get("L.Settings.LanguageChanged");
        Save();
    }

    [RelayCommand]
    private void SetLanguage(string lang) => SelectedLanguage = lang;

    private void Save()
    {
        try
        {
            // Tüm ayarlar korunur: yalnızca dil alanı değiştirilir.
            var settings = _settingsService.Load();
            settings.Language = SelectedLanguage;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings could not be saved.");
            StatusText = _localizer.Get("L.Common.Error.SettingsSave");
        }
    }
}
