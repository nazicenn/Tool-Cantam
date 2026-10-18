using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class SlugGeneratorServiceTests
{
    private readonly SlugGeneratorService _service = new();

    private static SlugOptions Opts(
        bool lower = true, char sep = '-', int max = 0, bool unicode = false)
        => new(lower, sep, max, unicode);

    [Fact]
    public async Task GenerateAsync_SimpleText_ReturnsSlug()
    {
        var r = await _service.GenerateAsync("Hello World", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("hello-world", r.Value!.Slug);
    }

    [Fact]
    public async Task GenerateAsync_TurkishChars_Transliterated()
    {
        var r = await _service.GenerateAsync("Merhaba Dünya ÇĞİÖŞÜ", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("merhaba-dunya-cgiosu", r.Value!.Slug);
    }

    [Fact]
    public async Task GenerateAsync_AccentedChars_Transliterated()
    {
        var r = await _service.GenerateAsync("Café déjà vu", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("cafe-deja-vu", r.Value!.Slug);
    }

    [Fact]
    public async Task GenerateAsync_NoLowercase_PreservesCase()
    {
        var r = await _service.GenerateAsync("Hello World", Opts(lower: false), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("Hello-World", r.Value!.Slug);
    }

    [Fact]
    public async Task GenerateAsync_UnderscoreSeparator_UsesUnderscore()
    {
        var r = await _service.GenerateAsync("hello world", Opts(sep: '_'), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("hello_world", r.Value!.Slug);
    }

    [Fact]
    public async Task GenerateAsync_SpecialCharsRemoved()
    {
        var r = await _service.GenerateAsync("hello! @world#", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("hello-world", r.Value!.Slug);
    }

    [Fact]
    public async Task GenerateAsync_MaxLength_Truncates()
    {
        var r = await _service.GenerateAsync("hello world test", Opts(max: 10), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.Slug.Length <= 10);
    }

    [Fact]
    public async Task GenerateAsync_Empty_ReturnsValidation()
    {
        var r = await _service.GenerateAsync("", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_AllSpecial_ReturnsEmptyResult()
    {
        var r = await _service.GenerateAsync("!!!@@@###", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidSeparator_ReturnsValidation()
    {
        var r = await _service.GenerateAsync("hello", Opts(sep: 'a'), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_UnicodeAllowed_PreservesChars()
    {
        var r = await _service.GenerateAsync("Merhaba Dünya", Opts(unicode: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("ü", r.Value!.Slug);
    }
}