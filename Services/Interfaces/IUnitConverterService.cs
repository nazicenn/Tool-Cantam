namespace tool_cantam.Services.Interfaces;

public interface IUnitConverterService
{
    double Convert(double value, string fromUnit, string toUnit, UnitCategory category);
    string[] GetUnits(UnitCategory category);
    UnitCategory[] GetCategories();
}

public enum UnitCategory
{
    Length,
    Weight,
    Temperature,
    Area,
    Volume,
    Speed,
    Time,
    Data
}