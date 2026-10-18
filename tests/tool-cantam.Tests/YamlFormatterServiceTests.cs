using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class YamlFormatterServiceTests
{
    private readonly YamlFormatterService _service = new();

    [Fact]
    public async Task FormatAsync_ValidYaml_Formats()
    {
        var r = await _service.FormatAsync("name: John\nage: 30", 2, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("name:", r.Value!.Output);
    }

    [Fact]
    public async Task FormatAsync_Empty_ReturnsValidation()
    {
        var r = await _service.FormatAsync("", 2, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task FormatAsync_InvalidYaml_ReturnsInvalidFormat()
    {
        var r = await _service.FormatAsync("- a\n: b: c", 2, CancellationToken.None);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public async Task MinifyAsync_ValidYaml_ReturnsCompact()
    {
        var r = await _service.MinifyAsync("name: John\nage: 30", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("name:", r.Value!.Output);
    }

    [Fact]
    public async Task MinifyAsync_Empty_ReturnsValidation()
    {
        var r = await _service.MinifyAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}