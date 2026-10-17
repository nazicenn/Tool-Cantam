using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class UnitConverterViewModel : ObservableObject
{
    private readonly IUnitConverterService _converterService;
    private readonly IClipboardService _clipboardService;

    /// <summary>
    /// Kategori değişirken birim listesi yenilenirken ara dönüşümlerin tetiklenmesini engeller.
    /// </summary>
    private bool _suppressConversion;

    /// <summary>
    /// Son dönüşümün yuvarlanmamış sonucu; birim takasında hassasiyet kaybını önler.
    /// </summary>
    private double? _lastResult;

    [ObservableProperty]
    private string _inputValue = "1";

    [ObservableProperty]
    private string _outputValue = "";

    [ObservableProperty]
    private string _fromUnit = "";

    [ObservableProperty]
    private string _toUnit = "";

    [ObservableProperty]
    private UnitCategory _selectedCategory = UnitCategory.Length;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private NotificationType _statusType = NotificationType.Info;

    public ObservableCollection<string> Units { get; } = new();
    public ObservableCollection<UnitCategory> Categories { get; } = new();

    public UnitConverterViewModel(
        IUnitConverterService converterService,
        IClipboardService clipboardService)
    {
        _converterService = converterService;
        _clipboardService = clipboardService;

        // Kategorileri yükle
        foreach (var cat in _converterService.GetCategories())
            Categories.Add(cat);

        LoadUnits();
    }

    partial void OnSelectedCategoryChanged(UnitCategory value)
    {
        _suppressConversion = true;
        try
        {
            LoadUnits();
        }
        finally
        {
            _suppressConversion = false;
        }

        Convert();
    }

    partial void OnInputValueChanged(string value)
    {
        if (!_suppressConversion) Convert();
    }

    partial void OnFromUnitChanged(string value)
    {
        if (!_suppressConversion) Convert();
    }

    partial void OnToUnitChanged(string value)
    {
        if (!_suppressConversion) Convert();
    }

    private void LoadUnits()
    {
        Units.Clear();
        var units = _converterService.GetUnits(SelectedCategory);
        foreach (var u in units)
            Units.Add(u);

        if (Units.Count >= 2)
        {
            FromUnit = Units[0];
            ToUnit = Units[1];
        }
        else
        {
            FromUnit = string.Empty;
            ToUnit = string.Empty;
        }
    }

    [RelayCommand]
    private void Convert()
    {
        if (string.IsNullOrWhiteSpace(InputValue) ||
            string.IsNullOrWhiteSpace(FromUnit) ||
            string.IsNullOrWhiteSpace(ToUnit))
        {
            OutputValue = "";
            _lastResult = null;
            return;
        }

        if (!TryParseNumber(InputValue, out double value))
        {
            OutputValue = "";
            _lastResult = null;
            StatusText = "✗ Invalid number format";
            StatusType = NotificationType.Error;
            return;
        }

        try
        {
            double result = _converterService.Convert(value, FromUnit, ToUnit, SelectedCategory);
            _lastResult = result;
            OutputValue = FormatResult(result);
            StatusText = $"✓ {FormatResult(value)} {FromUnit} = {OutputValue} {ToUnit}";
            StatusType = NotificationType.Success;
        }
        catch (Exception ex)
        {
            OutputValue = "";
            _lastResult = null;
            StatusText = $"✗ {ex.Message}";
            StatusType = NotificationType.Error;
        }
    }

    /// <summary>
    /// Girdiyi kültürden bağımsız olarak çözer: "1.5" her ortamda 1.5'tir.
    /// Yerel ondalık ayırıcı nokta değilse (ör. tr-TR'de virgül) o biçim de kabul edilir.
    /// Binlik ayırıcı kabul edilmez; böylece "1,234" gibi belirsiz girdiler sessizce yanlış yorumlanmaz.
    /// </summary>
    private static bool TryParseNumber(string text, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();

        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        var current = CultureInfo.CurrentCulture;
        if (!string.Equals(current.NumberFormat.NumberDecimalSeparator, ".", StringComparison.Ordinal))
            return double.TryParse(trimmed, NumberStyles.Float, current, out value);

        return false;
    }

    /// <summary>
    /// Teknik bir araç olduğu için çıktı her ortamda aynıdır: ondalık ayırıcı nokta.
    /// </summary>
    private static string FormatResult(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return "Invalid";

        // Küçük sayılar için bilimsel gösterim
        if (Math.Abs(value) > 0 && Math.Abs(value) < 0.0001)
            return value.ToString("0.######E+0", CultureInfo.InvariantCulture);

        // Büyük sayılar için bilimsel gösterim
        if (Math.Abs(value) > 1e15)
            return value.ToString("0.######E+0", CultureInfo.InvariantCulture);

        // Normal gösterim (max 6 ondalık)
        return Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void SwapUnits()
    {
        if (string.IsNullOrWhiteSpace(FromUnit) || string.IsNullOrWhiteSpace(ToUnit))
            return;

        double? previousResult = _lastResult;

        var temp = FromUnit;
        FromUnit = ToUnit;
        ToUnit = temp;

        // Yuvarlanmış çıktı yerine tam hassasiyetli sonucu girdiye taşı.
        if (previousResult is { } result && !double.IsNaN(result) && !double.IsInfinity(result))
        {
            InputValue = FormatResult(result);
        }

        UpdateStatus("↻ Units swapped", NotificationType.Info);
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (string.IsNullOrWhiteSpace(OutputValue))
        {
            UpdateStatus("Nothing to copy", NotificationType.Warning);
            return;
        }

        _clipboardService.CopyToClipboard($"{OutputValue} {ToUnit}");
        UpdateStatus("✓ Result copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void Clear()
    {
        InputValue = "1";
        OutputValue = "";
        _lastResult = null;
        UpdateStatus("✓ Cleared", NotificationType.Success);
    }

    private void UpdateStatus(string message, NotificationType type)
    {
        StatusText = message;
        StatusType = type;
    }
}