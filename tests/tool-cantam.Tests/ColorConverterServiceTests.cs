using tool_cantam.Services;
using Xunit;

namespace ToolCantam.Tests;

public class ColorConverterServiceTests
{
    private readonly ColorConverterService _service = new();

    [Theory]
    [InlineData(255, 0, 0, "#FF0000")]
    [InlineData(0, 255, 0, "#00FF00")]
    [InlineData(0, 0, 0, "#000000")]
    [InlineData(255, 255, 255, "#FFFFFF")]
    [InlineData(18, 52, 86, "#123456")]
    public void RgbToHex_formats_six_digits(int r, int g, int b, string expected)
    {
        Assert.Equal(expected, _service.RgbToHex(r, g, b));
    }

    [Fact]
    public void RgbToHex_clamps_out_of_range_channels()
    {
        Assert.Equal("#00FF00", _service.RgbToHex(-10, 300, 0));
        Assert.Equal("#000000", _service.RgbToHex(-1, -1, -1));
    }

    [Theory]
    [InlineData("#FF8800", 255, 136, 0)]
    [InlineData("FF8800", 255, 136, 0)]
    [InlineData("#f80", 255, 136, 0)]
    [InlineData("  #Ff8800  ", 255, 136, 0)]
    public void TryParseHex_accepts_common_forms(string input, int expectedR, int expectedG, int expectedB)
    {
        Assert.True(_service.TryParseHex(input, out int r, out int g, out int b));
        Assert.Equal((expectedR, expectedG, expectedB), (r, g, b));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("#")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    [InlineData("not a color")]
    [InlineData("##123")]
    [InlineData("#12 456")]
    public void TryParseHex_rejects_invalid_input_without_throwing(string? input)
    {
        Assert.False(_service.TryParseHex(input!, out _, out _, out _));
    }

    [Fact]
    public void HexToRgb_throws_on_invalid_input()
    {
        Assert.Throws<FormatException>(() => _service.HexToRgb("nope"));
    }

    [Theory]
    [InlineData(255, 0, 0, "hsl(0, 100%, 50%)")]
    [InlineData(0, 0, 0, "hsl(0, 0%, 0%)")]
    [InlineData(255, 255, 255, "hsl(0, 0%, 100%)")]
    [InlineData(0, 255, 0, "hsl(120, 100%, 50%)")]
    [InlineData(0, 0, 255, "hsl(240, 100%, 50%)")]
    public void RgbToHsl_returns_expected_value(int r, int g, int b, string expected)
    {
        Assert.Equal(expected, _service.RgbToHsl(r, g, b));
    }

    [Theory]
    [InlineData(0, 100, 50, 255, 0, 0)]
    [InlineData(120, 100, 50, 0, 255, 0)]
    [InlineData(240, 100, 50, 0, 0, 255)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(0, 0, 100, 255, 255, 255)]
    public void HslToRgb_returns_expected_value(double h, double s, double l, int r, int g, int b)
    {
        Assert.Equal((r, g, b), _service.HslToRgb(h, s, l));
    }

    [Fact]
    public void Hex_round_trip_preserves_every_channel()
    {
        for (int r = 0; r <= 255; r += 17)
        {
            for (int g = 0; g <= 255; g += 51)
            {
                for (int b = 0; b <= 255; b += 85)
                {
                    string hex = _service.RgbToHex(r, g, b);
                    Assert.True(_service.TryParseHex(hex, out int rr, out int gg, out int bb));
                    Assert.Equal((r, g, b), (rr, gg, bb));
                }
            }
        }
    }
}
