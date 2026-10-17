namespace tool_cantam.Services.Interfaces;

public interface IColorConverterService
{
    string RgbToHex(int r, int g, int b);
    string RgbToHsl(int r, int g, int b);
    (int R, int G, int B) HexToRgb(string hex);
    (int R, int G, int B) HslToRgb(double h, double s, double l);
    bool TryParseHex(string hex, out int r, out int g, out int b);
}