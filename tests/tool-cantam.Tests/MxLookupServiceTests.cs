using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class MxLookupServiceTests
{
    private readonly MxLookupService _service = new();

    [Fact]
    public async Task LookupAsync_EmptyDomain_ReturnsValidation()
    {
        var r = await _service.LookupAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_InvalidDomain_ReturnsValidation()
    {
        var r = await _service.LookupAsync("not_a_domain", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_ValidDomain_ReturnsSuccess()
    {
        var r = await _service.LookupAsync("example.com", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.NotNull(r.Value);
    }

    [Fact]
    public async Task LookupAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.LookupAsync("example.com", cts.Token);
        Assert.False(r.IsSuccess);
    }
}