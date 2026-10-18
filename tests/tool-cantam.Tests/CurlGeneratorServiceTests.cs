using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class CurlGeneratorServiceTests
{
    private readonly CurlGeneratorService _service = new();

    private static CurlOptions Opts(
        string url = "https://example.com", string method = "GET",
        string headers = "", string? body = null,
        bool incHeaders = true, bool incBody = true,
        bool longFlags = false, bool insecure = false)
        => new(url, method, headers, body, incHeaders, incBody, longFlags, insecure);

    [Fact]
    public async Task GenerateAsync_Get_CreatesSimpleCurl()
    {
        var r = await _service.GenerateAsync(Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("curl", r.Value!.Command);
        Assert.Contains("https://example.com", r.Value.Command);
        Assert.DoesNotContain("-X GET", r.Value.Command);
    }

    [Fact]
    public async Task GenerateAsync_Post_IncludesMethodAndBody()
    {
        var r = await _service.GenerateAsync(
            Opts(method: "POST", body: "{\"a\":1}"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("-X POST", r.Value!.Command);
        Assert.Contains("-d ", r.Value.Command);
    }

    [Fact]
    public async Task GenerateAsync_Headers_Included()
    {
        var r = await _service.GenerateAsync(
            Opts(headers: "Content-Type: application/json"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("-H ", r.Value!.Command);
        Assert.Contains("Content-Type", r.Value.Command);
    }

    [Fact]
    public async Task GenerateAsync_LongFlags_UsesLongForm()
    {
        var r = await _service.GenerateAsync(
            Opts(method: "POST", body: "{}", longFlags: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("--request POST", r.Value!.Command);
        Assert.Contains("--data", r.Value.Command);
    }

    [Fact]
    public async Task GenerateAsync_Insecure_AddsFlag()
    {
        var r = await _service.GenerateAsync(Opts(insecure: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("-k", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_EmptyUrl_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(url: ""), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidUrl_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(url: "not-a-url"), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}