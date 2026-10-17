using System;
using System.Collections.Generic;
using System.Linq;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class UnitConverterService : IUnitConverterService
{
    // Base unit: meter
    private static readonly Dictionary<string, double> LengthUnits = new()
    {
        { "Millimeter (mm)", 0.001 },
        { "Centimeter (cm)", 0.01 },
        { "Meter (m)", 1.0 },
        { "Kilometer (km)", 1000.0 },
        { "Inch (in)", 0.0254 },
        { "Foot (ft)", 0.3048 },
        { "Yard (yd)", 0.9144 },
        { "Mile (mi)", 1609.344 },
        { "Nautical Mile (nmi)", 1852.0 }
    };

    // Base unit: kilogram
    private static readonly Dictionary<string, double> WeightUnits = new()
    {
        { "Milligram (mg)", 0.000001 },
        { "Gram (g)", 0.001 },
        { "Kilogram (kg)", 1.0 },
        { "Metric Ton (t)", 1000.0 },
        { "Ounce (oz)", 0.028349523125 },
        { "Pound (lb)", 0.45359237 },
        { "Stone (st)", 6.35029318 }
    };

    // Base unit: Celsius (special handling)
    private static readonly string[] TemperatureUnits = new[]
    {
        "Celsius (°C)",
        "Fahrenheit (°F)",
        "Kelvin (K)"
    };

    // Base unit: square meter
    private static readonly Dictionary<string, double> AreaUnits = new()
    {
        { "Square Millimeter (mm²)", 0.000001 },
        { "Square Centimeter (cm²)", 0.0001 },
        { "Square Meter (m²)", 1.0 },
        { "Square Kilometer (km²)", 1000000.0 },
        { "Hectare (ha)", 10000.0 },
        { "Acre (ac)", 4046.8564224 },
        { "Square Inch (in²)", 0.00064516 },
        { "Square Foot (ft²)", 0.09290304 },
        { "Square Yard (yd²)", 0.83612736 },
        { "Square Mile (mi²)", 2589988.110336 }
    };

    // Base unit: liter
    private static readonly Dictionary<string, double> VolumeUnits = new()
    {
        { "Milliliter (ml)", 0.001 },
        { "Liter (L)", 1.0 },
        { "Cubic Meter (m³)", 1000.0 },
        { "Gallon (US)", 3.785411784 },
        { "Quart (US)", 0.946352946 },
        { "Pint (US)", 0.473176473 },
        { "Cup (US)", 0.2365882365 },
        { "Fluid Ounce (US)", 0.0295735295625 },
        { "Tablespoon (US)", 0.01478676478125 },
        { "Teaspoon (US)", 0.00492892159375 }
    };

    // Base unit: m/s
    private static readonly Dictionary<string, double> SpeedUnits = new()
    {
        { "Meter/second (m/s)", 1.0 },
        { "Kilometer/hour (km/h)", 1.0 / 3.6 },
        { "Mile/hour (mph)", 0.44704 },
        { "Foot/second (ft/s)", 0.3048 },
        { "Knot (kn)", 1852.0 / 3600.0 }
    };

    // Base unit: second
    private static readonly Dictionary<string, double> TimeUnits = new()
    {
        { "Millisecond (ms)", 0.001 },
        { "Second (s)", 1.0 },
        { "Minute (min)", 60.0 },
        { "Hour (h)", 3600.0 },
        { "Day (d)", 86400.0 },
        { "Week (wk)", 604800.0 },
        { "Month (mo)", 2629746.0 },
        { "Year (yr)", 31556952.0 }
    };

    // Base unit: byte
    private static readonly Dictionary<string, double> DataUnits = new()
    {
        { "Bit (b)", 0.125 },
        { "Byte (B)", 1.0 },
        // 1024 tabanlı (IEC) birimler; etiketler de buna göre KiB/MiB... olarak verilir.
        { "Kibibyte (KiB)", 1024.0 },
        { "Mebibyte (MiB)", 1048576.0 },
        { "Gibibyte (GiB)", 1073741824.0 },
        { "Tebibyte (TiB)", 1099511627776.0 },
        { "Pebibyte (PiB)", 1125899906842624.0 }
    };

    public double Convert(double value, string fromUnit, string toUnit, UnitCategory category)
    {
        if (fromUnit == toUnit) return value;

        return category switch
        {
            UnitCategory.Length => ConvertSimple(value, fromUnit, toUnit, LengthUnits),
            UnitCategory.Weight => ConvertSimple(value, fromUnit, toUnit, WeightUnits),
            UnitCategory.Temperature => ConvertTemperature(value, fromUnit, toUnit),
            UnitCategory.Area => ConvertSimple(value, fromUnit, toUnit, AreaUnits),
            UnitCategory.Volume => ConvertSimple(value, fromUnit, toUnit, VolumeUnits),
            UnitCategory.Speed => ConvertSimple(value, fromUnit, toUnit, SpeedUnits),
            UnitCategory.Time => ConvertSimple(value, fromUnit, toUnit, TimeUnits),
            UnitCategory.Data => ConvertSimple(value, fromUnit, toUnit, DataUnits),
            _ => throw new ArgumentException($"Unknown category: {category}")
        };
    }

    private double ConvertSimple(double value, string fromUnit, string toUnit, Dictionary<string, double> units)
    {
        if (!units.ContainsKey(fromUnit))
            throw new ArgumentException($"Unknown unit: {fromUnit}");
        if (!units.ContainsKey(toUnit))
            throw new ArgumentException($"Unknown unit: {toUnit}");

        double fromFactor = units[fromUnit];
        double toFactor = units[toUnit];
        return value * fromFactor / toFactor;
    }

    private double ConvertTemperature(double value, string from, string to)
    {
        // Önce Celsius'a çevir
        double celsius = from switch
        {
            "Celsius (°C)" => value,
            "Fahrenheit (°F)" => (value - 32) * 5 / 9,
            "Kelvin (K)" => value - 273.15,
            _ => throw new ArgumentException($"Unknown temperature unit: {from}")
        };

        // Sonra hedef birime çevir
        return to switch
        {
            "Celsius (°C)" => celsius,
            "Fahrenheit (°F)" => celsius * 9 / 5 + 32,
            "Kelvin (K)" => celsius + 273.15,
            _ => throw new ArgumentException($"Unknown temperature unit: {to}")
        };
    }

    public string[] GetUnits(UnitCategory category)
    {
        return category switch
        {
            UnitCategory.Length => LengthUnits.Keys.ToArray(),
            UnitCategory.Weight => WeightUnits.Keys.ToArray(),
            UnitCategory.Temperature => TemperatureUnits,
            UnitCategory.Area => AreaUnits.Keys.ToArray(),
            UnitCategory.Volume => VolumeUnits.Keys.ToArray(),
            UnitCategory.Speed => SpeedUnits.Keys.ToArray(),
            UnitCategory.Time => TimeUnits.Keys.ToArray(),
            UnitCategory.Data => DataUnits.Keys.ToArray(),
            _ => Array.Empty<string>()
        };
    }

    public UnitCategory[] GetCategories()
    {
        return Enum.GetValues<UnitCategory>();
    }
}