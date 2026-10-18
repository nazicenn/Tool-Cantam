using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class CssGradientGeneratorServiceTests
{
    private readonly CssGradientGeneratorService _service = new();

    private static GradientOptions Opts(string c1 = "#FF0000", string c2 = "#00FF00", GradientDirection d = GradientDirection.ToRight)
        => new(c1, c2, d);

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsCss()
    {
        var r = await _service.GenerateAsync(Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("linear-gradient", r.Value!.Css);
        Assert.Contains("#FF0000", r.Value.Css);
    }

    [Fact]
    public async Task GenerateAsync_ShortHex_Accepted()
    {
        var r = await _service.GenerateAsync(Opts(c1: "#F00", c2: "#0F0"), CancellationToken.None);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public async Task GenerateAsync_InvalidColor_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(c1: "red"), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_SameColors_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(c1: "#FF0000", c2: "#FF0000"), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_DifferentDirections_Work()
    {
        foreach (var d in new[] { GradientDirection.ToRight, GradientDirection.ToLeft, GradientDirection.ToBottom, GradientDirection.ToTop })
        {
            var r = await _service.GenerateAsync(Opts(d: d), CancellationToken.None);
            Assert.True(r.IsSuccess);
        }
    }
}