using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class RestApiServiceTests
{
    private readonly RestApiService _service = new();
    private static RestApiOptions Opts(string url = "https://example.com", string method = "GET") => new(url, method, "", null);

    [Fact] public async Task SendAsync_EmptyUrl_ReturnsValidation() { var r = await _service.SendAsync(Opts(url: ""), TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task SendAsync_InvalidUrl_ReturnsValidation() { var r = await _service.SendAsync(Opts(url: "not-a-url"), TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task SendAsync_InvalidMethod_ReturnsValidation() { var r = await _service.SendAsync(Opts(method: "INVALID"), TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task SendAsync_Cancelled_ReturnsFailure() { using var cts = new CancellationTokenSource(); cts.Cancel(); var r = await _service.SendAsync(Opts(), cts.Token); Assert.False(r.IsSuccess); }
}