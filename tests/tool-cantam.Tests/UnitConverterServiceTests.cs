using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Birim dönüşümünün doğruluğunu kilitler. Dönüşüm faktörleri yaklaşık değil,
/// tanımlı (exact) değerlerdir; bu yüzden gidiş-dönüş kayıpsız olmalıdır.
/// </summary>
public class UnitConverterServiceTests
{
    private readonly UnitConverterService _service = new();

    private const double Tolerance = 1e-9;

    [Theory]
    [InlineData("Meter (m)", "Centimeter (cm)", 1.0, 100.0)]
    [InlineData("Kilometer (km)", "Meter (m)", 1.0, 1000.0)]
    [InlineData("Mile (mi)", "Meter (m)", 1.0, 1609.344)]
    [InlineData("Inch (in)", "Centimeter (cm)", 1.0, 2.54)]
    [InlineData("Foot (ft)", "Inch (in)", 1.0, 12.0)]
    [InlineData("Nautical Mile (nmi)", "Meter (m)", 1.0, 1852.0)]
    public void Length_conversions_are_exact(string from, string to, double value, double expected)
    {
        double actual = _service.Convert(value, from, to, UnitCategory.Length);
        Assert.Equal(expected, actual, Tolerance);
    }

    [Theory]
    [InlineData("Pound (lb)", "Kilogram (kg)", 1.0, 0.45359237)]
    [InlineData("Ounce (oz)", "Gram (g)", 1.0, 28.349523125)]
    [InlineData("Stone (st)", "Pound (lb)", 1.0, 14.0)]
    [InlineData("Metric Ton (t)", "Kilogram (kg)", 1.0, 1000.0)]
    public void Weight_conversions_are_exact(string from, string to, double value, double expected)
    {
        double actual = _service.Convert(value, from, to, UnitCategory.Weight);
        Assert.Equal(expected, actual, Tolerance);
    }

    [Theory]
    [InlineData("Celsius (°C)", "Fahrenheit (°F)", 100.0, 212.0)]
    [InlineData("Celsius (°C)", "Kelvin (K)", 0.0, 273.15)]
    [InlineData("Fahrenheit (°F)", "Celsius (°C)", 32.0, 0.0)]
    [InlineData("Fahrenheit (°F)", "Celsius (°C)", -40.0, -40.0)]
    [InlineData("Kelvin (K)", "Celsius (°C)", 0.0, -273.15)]
    public void Temperature_conversions_are_correct(string from, string to, double value, double expected)
    {
        double actual = _service.Convert(value, from, to, UnitCategory.Temperature);
        Assert.Equal(expected, actual, 1e-9);
    }

    [Fact]
    public void Speed_conversions_use_exact_definitions()
    {
        // 1 km/h tam olarak 1/3.6 m/s olmalı; yuvarlanmış 0.277778 kabul edilmez.
        double metersPerSecond = _service.Convert(1.0, "Kilometer/hour (km/h)", "Meter/second (m/s)", UnitCategory.Speed);
        Assert.Equal(1.0 / 3.6, metersPerSecond, Tolerance);

        // 3.6 km/h = 1 m/s
        double exact = _service.Convert(3.6, "Kilometer/hour (km/h)", "Meter/second (m/s)", UnitCategory.Speed);
        Assert.Equal(1.0, exact, Tolerance);

        // 1 knot = 1852 m / 3600 s
        double knot = _service.Convert(1.0, "Knot (kn)", "Meter/second (m/s)", UnitCategory.Speed);
        Assert.Equal(1852.0 / 3600.0, knot, Tolerance);
    }

    [Fact]
    public void Data_units_are_binary_and_labelled_as_such()
    {
        Assert.Equal(1024.0, _service.Convert(1.0, "Kibibyte (KiB)", "Byte (B)", UnitCategory.Data), Tolerance);
        Assert.Equal(8.0, _service.Convert(1.0, "Byte (B)", "Bit (b)", UnitCategory.Data), Tolerance);
        Assert.Equal(1048576.0, _service.Convert(1.0, "Mebibyte (MiB)", "Byte (B)", UnitCategory.Data), Tolerance);

        // Etiketler gerçeği söylemeli: 1024 tabanlı birimler KiB/MiB olarak sunulur.
        string[] units = _service.GetUnits(UnitCategory.Data);
        Assert.Contains("Kibibyte (KiB)", units);
        Assert.DoesNotContain("Kilobyte (KB)", units);
    }

    [Theory]
    [InlineData(UnitCategory.Length, "Mile (mi)", "Kilometer (km)")]
    [InlineData(UnitCategory.Volume, "Gallon (US)", "Liter (L)")]
    [InlineData(UnitCategory.Area, "Acre (ac)", "Square Meter (m²)")]
    [InlineData(UnitCategory.Weight, "Ounce (oz)", "Gram (g)")]
    [InlineData(UnitCategory.Time, "Week (wk)", "Day (d)")]
    public void Round_trip_conversion_returns_the_original_value(UnitCategory category, string unitA, string unitB)
    {
        const double original = 12.3456789;

        double forward = _service.Convert(original, unitA, unitB, category);
        double back = _service.Convert(forward, unitB, unitA, category);

        Assert.Equal(original, back, 1e-9);
    }

    [Fact]
    public void Same_unit_returns_value_unchanged()
    {
        Assert.Equal(42.0, _service.Convert(42.0, "Meter (m)", "Meter (m)", UnitCategory.Length), Tolerance);
    }

    [Fact]
    public void Unknown_unit_is_rejected_instead_of_silently_returning_a_wrong_value()
    {
        Assert.Throws<ArgumentException>(() =>
            _service.Convert(1.0, "Smoot", "Meter (m)", UnitCategory.Length));

        Assert.Throws<ArgumentException>(() =>
            _service.Convert(1.0, "Celsius (°C)", "Rankine (°R)", UnitCategory.Temperature));
    }

    [Fact]
    public void Every_category_exposes_at_least_two_units()
    {
        foreach (UnitCategory category in _service.GetCategories())
        {
            string[] units = _service.GetUnits(category);
            Assert.True(units.Length >= 2, $"{category} kategorisinde dönüştürülebilir en az iki birim olmalı.");
        }
    }
}
