using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class UnitConverterViewModelTests
{
    // -------- Gerçek UnitConverterService (built-in, mock gerekmez) --------
    private sealed class FakeClipboardService : IClipboardService
    {
        public string? LastCopied { get; private set; }

        public void CopyToClipboard(string text) => LastCopied = text;
        public Task<string?> GetFromClipboardAsync() => Task.FromResult<string?>(null);
        public bool ContainsText() => LastCopied is not null;
    }

    private static UnitConverterViewModel Create(out FakeClipboardService clipboard)
    {
        clipboard = new FakeClipboardService();
        // Gerçek servis — saf, built-in, yan etkisiz
        var service = new UnitConverterService();
        return new UnitConverterViewModel(service, clipboard);
    }

    // -------- Kategori / Birim yükleme --------

    [Fact]
    public void Categories_PopulatedOnConstruction()
    {
        var vm = Create(out _);
        Assert.NotEmpty(vm.Categories);
        Assert.Contains(UnitCategory.Length, vm.Categories);
        Assert.Contains(UnitCategory.Temperature, vm.Categories);
    }

    [Fact]
    public void Units_PopulatedOnConstruction()
    {
        var vm = Create(out _);
        Assert.NotEmpty(vm.Units);
        Assert.Contains("Meter (m)", vm.Units);
    }

    [Fact]
    public void FromUnit_ToUnit_AutoSetToFirstTwo()
    {
        var vm = Create(out _);
        Assert.False(string.IsNullOrEmpty(vm.FromUnit));
        Assert.False(string.IsNullOrEmpty(vm.ToUnit));
        Assert.NotEqual(vm.FromUnit, vm.ToUnit);
    }

    // -------- Dönüşüm --------

    [Fact]
    public void Convert_ValidLength_ProducesOutput()
    {
        var vm = Create(out _);
        vm.FromUnit = "Meter (m)";
        vm.ToUnit = "Centimeter (cm)";
        vm.InputValue = "1";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("100", vm.OutputValue);
    }

    [Fact]
    public void Convert_KilometerToMeter_Produces1000()
    {
        var vm = Create(out _);
        vm.FromUnit = "Kilometer (km)";
        vm.ToUnit = "Meter (m)";
        vm.InputValue = "1";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("1000", vm.OutputValue);
    }

    [Fact]
    public void Convert_Temperature_CelsiusToFahrenheit()
    {
        var vm = Create(out _);
        vm.SelectedCategory = UnitCategory.Temperature;
        vm.FromUnit = "Celsius (°C)";
        vm.ToUnit = "Fahrenheit (°F)";
        vm.InputValue = "0";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("32", vm.OutputValue);
    }

    [Fact]
    public void Convert_Temperature_FahrenheitToCelsius()
    {
        var vm = Create(out _);
        vm.SelectedCategory = UnitCategory.Temperature;
        vm.FromUnit = "Fahrenheit (°F)";
        vm.ToUnit = "Celsius (°C)";
        vm.InputValue = "32";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("0", vm.OutputValue);
    }

    [Fact]
    public void Convert_Temperature_KelvinToCelsius()
    {
        var vm = Create(out _);
        vm.SelectedCategory = UnitCategory.Temperature;
        vm.FromUnit = "Kelvin (K)";
        vm.ToUnit = "Celsius (°C)";
        vm.InputValue = "273.15";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("0", vm.OutputValue);
    }

    [Fact]
    public void Convert_InvalidNumber_ShowsError()
    {
        var vm = Create(out _);
        vm.InputValue = "abc";
        vm.ConvertCommand.Execute(null);

        Assert.Empty(vm.OutputValue);
        Assert.Equal(NotificationType.Error, vm.StatusType);
    }

    [Fact]
    public void Convert_EmptyInput_ProducesEmpty()
    {
        var vm = Create(out _);
        vm.InputValue = "";
        vm.ConvertCommand.Execute(null);

        Assert.Empty(vm.OutputValue);
    }

    [Fact]
    public void Convert_SameUnit_ReturnsSameValue()
    {
        var vm = Create(out _);
        vm.FromUnit = "Meter (m)";
        vm.ToUnit = "Meter (m)";
        vm.InputValue = "5";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("5", vm.OutputValue);
    }

    [Fact]
    public void Convert_DataBytesToKibibytes()
    {
        var vm = Create(out _);
        vm.SelectedCategory = UnitCategory.Data;
        vm.FromUnit = "Byte (B)";
        vm.ToUnit = "Kibibyte (KiB)";
        vm.InputValue = "1024";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("1", vm.OutputValue);
    }

    // -------- Otomatik dönüşüm --------

    [Fact]
    public void OnInputValueChanged_TriggersConvert()
    {
        var vm = Create(out _);
        vm.FromUnit = "Meter (m)";
        vm.ToUnit = "Centimeter (cm)";
        vm.InputValue = "2";

        Assert.Equal("200", vm.OutputValue);
    }

    [Fact]
    public void OnFromUnitChanged_TriggersConvert()
    {
        var vm = Create(out _);
        vm.InputValue = "1";
        vm.FromUnit = "Kilometer (km)";

        // Auto-convert tetiklendi — çıktı boş değil
        Assert.False(string.IsNullOrEmpty(vm.OutputValue));
    }

    [Fact]
    public void OnCategoryChanged_ReloadsUnits()
    {
        var vm = Create(out _);
        vm.SelectedCategory = UnitCategory.Temperature;

        Assert.Contains("Celsius (°C)", vm.Units);
        Assert.Contains("Fahrenheit (°F)", vm.Units);
    }

    // -------- Swap --------

    [Fact]
    public void SwapUnits_SwapsFromTo()
    {
        var vm = Create(out _);
        vm.FromUnit = "Meter (m)";
        vm.ToUnit = "Centimeter (cm)";
        vm.InputValue = "1";
        vm.ConvertCommand.Execute(null);

        vm.SwapUnitsCommand.Execute(null);

        Assert.Equal("Centimeter (cm)", vm.FromUnit);
        Assert.Equal("Meter (m)", vm.ToUnit);
    }

    [Fact]
    public void SwapUnits_EmptyUnits_DoesNothing()
    {
        var vm = Create(out _);
        vm.FromUnit = "";
        vm.ToUnit = "";

        vm.SwapUnitsCommand.Execute(null);

        Assert.Equal("", vm.FromUnit);
        Assert.Equal("", vm.ToUnit);
    }

    // -------- Clipboard --------

    [Fact]
    public void CopyResult_ValidOutput_Copies()
    {
        var vm = Create(out var clipboard);
        vm.FromUnit = "Meter (m)";
        vm.ToUnit = "Centimeter (cm)";
        vm.InputValue = "1";
        vm.ConvertCommand.Execute(null);

        vm.CopyResultCommand.Execute(null);

        Assert.NotNull(clipboard.LastCopied);
        Assert.Contains("100", clipboard.LastCopied);
        Assert.Contains("Centimeter (cm)", clipboard.LastCopied);
    }

    [Fact]
    public void CopyResult_EmptyOutput_Warns()
    {
        var vm = Create(out var clipboard);
        vm.OutputValue = "";

        vm.CopyResultCommand.Execute(null);

        Assert.Null(clipboard.LastCopied);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
    }

    // -------- Clear --------

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _);
        vm.InputValue = "42";
        vm.ConvertCommand.Execute(null);

        vm.ClearCommand.Execute(null);

        Assert.Equal("1", vm.InputValue);
        Assert.Empty(vm.OutputValue);
    }

    // -------- Kültür / Sayı ayrıştırma (dolaylı) --------

    [Fact]
    public void Convert_InvariantCulture_DecimalPoint()
    {
        var vm = Create(out _);
        vm.FromUnit = "Meter (m)";
        vm.ToUnit = "Centimeter (cm)";
        vm.InputValue = "1.5";
        vm.ConvertCommand.Execute(null);

        Assert.Equal("150", vm.OutputValue);
    }

    [Fact]
    public void Convert_TurkishCulture_DecimalComma()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var vm = Create(out _);
            vm.FromUnit = "Meter (m)";
            vm.ToUnit = "Centimeter (cm)";
            vm.InputValue = "1,5";
            vm.ConvertCommand.Execute(null);

            Assert.Equal("150", vm.OutputValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // -------- Formatlama (dolaylı) --------

    [Fact]
    public void Convert_SmallValue_ScientificNotation()
    {
        var vm = Create(out _);
        vm.FromUnit = "Millimeter (mm)";
        vm.ToUnit = "Kilometer (km)";
        vm.InputValue = "0.001";
        vm.ConvertCommand.Execute(null);

        // 0.001 mm = 1e-9 km → bilimsel gösterim
        Assert.Contains("E", vm.OutputValue);
    }

    [Fact]
    public void Convert_LargeValue_ScientificNotation()
    {
        var vm = Create(out _);
        vm.FromUnit = "Kilometer (km)";
        vm.ToUnit = "Millimeter (mm)";
        vm.InputValue = "1e10";
        vm.ConvertCommand.Execute(null);

        // 1e10 km = 1e16 mm → bilimsel gösterim
        Assert.Contains("E", vm.OutputValue);
    }
}