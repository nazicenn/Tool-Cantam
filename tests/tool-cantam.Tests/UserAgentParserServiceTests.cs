using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class UserAgentParserServiceTests
{
    private readonly UserAgentParserService _service = new();

    [Fact] public async Task ParseAsync_ChromeWindows_ReturnsChrome() { var r = await _service.ParseAsync("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal("Chrome", r.Value!.Browser); Assert.Contains("Windows", r.Value.OperatingSystem); }
    [Fact] public async Task ParseAsync_Firefox_ReturnsFirefox() { var r = await _service.ParseAsync("Mozilla/5.0 (Windows NT 10.0; rv:109.0) Gecko/20100101 Firefox/119.0", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal("Firefox", r.Value!.Browser); }
    [Fact] public async Task ParseAsync_iPhone_IsMobile() { var r = await _service.ParseAsync("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile/15E148 Safari/604.1", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.True(r.Value!.IsMobile); Assert.Equal("iPhone", r.Value.DeviceType); }
    [Fact] public async Task ParseAsync_Bot_Detected() { var r = await _service.ParseAsync("Googlebot/2.1 (+http://www.google.com/bot.html)", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.True(r.Value!.IsBot); }
    [Fact] public async Task ParseAsync_Empty_ReturnsValidation() { var r = await _service.ParseAsync("", TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
}