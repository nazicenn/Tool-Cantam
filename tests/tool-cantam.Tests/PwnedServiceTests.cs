using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class PwnedServiceTests
{
    private readonly PwnedService _service = new();

    [Fact] public async Task CheckPasswordAsync_Empty_ReturnsValidation() { var r = await _service.CheckPasswordAsync("", TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task CheckPasswordAsync_Null_ReturnsValidation() { var r = await _service.CheckPasswordAsync(null, TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task CheckPasswordAsync_Cancelled_ReturnsFailure() { using var cts = new CancellationTokenSource(); cts.Cancel(); var r = await _service.CheckPasswordAsync("test", cts.Token); Assert.False(r.IsSuccess); }
}