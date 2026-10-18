using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class HttpHeaderServiceTests
{
    private readonly HttpHeaderService _service = new();

    [Fact]
    public async Task AnalyzeAsync_EmptyUrl_ReturnsValidation()
    {
        var r = await _service.AnalyzeAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task AnalyzeAsync_NullUrl_ReturnsValidation()
    {
        var r = await _service.AnalyzeAsync(null, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidUrl_ReturnsValidation()
    {
        var r = await _service.AnalyzeAsync("not-a-url", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task AnalyzeAsync_NonHttpScheme_ReturnsValidation()
    {
        var r = await _service.AnalyzeAsync("ftp://example.com", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void IsValidUrl_ValidatesScheme()
    {
        Assert.True(_service.IsValidUrl("https://example.com"));
        Assert.True(_service.IsValidUrl("http://example.com"));
        Assert.False(_service.IsValidUrl("ftp://example.com"));
        Assert.False(_service.IsValidUrl(""));
        Assert.False(_service.IsValidUrl(null));
    }
}