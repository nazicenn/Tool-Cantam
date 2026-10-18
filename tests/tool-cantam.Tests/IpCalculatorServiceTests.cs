using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class IpCalculatorServiceTests
{
    private readonly IpCalculatorService _service = new();

    [Fact]
    public async Task CalculateAsync_Slash24_ReturnsCorrect()
    {
        var r = await _service.CalculateAsync("192.168.1.100", 24, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("192.168.1.0", r.Value!.Network);
        Assert.Equal("192.168.1.255", r.Value.Broadcast);
        Assert.Equal("255.255.255.0", r.Value.SubnetMask);
        Assert.Equal("192.168.1.1", r.Value.FirstUsable);
        Assert.Equal("192.168.1.254", r.Value.LastUsable);
        Assert.Equal(256, r.Value.TotalHosts);
        Assert.Equal(254, r.Value.UsableHosts);
    }

    [Fact]
    public async Task CalculateAsync_Slash32_SingleHost()
    {
        var r = await _service.CalculateAsync("10.0.0.1", 32, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("10.0.0.1", r.Value!.Network);
        Assert.Equal(0, r.Value.UsableHosts);
    }

    [Fact]
    public async Task CalculateAsync_PrivateIp_Detected()
    {
        var r = await _service.CalculateAsync("10.0.0.1", 24, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.IsPrivate);
    }

    [Fact]
    public async Task CalculateAsync_PublicIp_NotPrivate()
    {
        var r = await _service.CalculateAsync("8.8.8.8", 24, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.False(r.Value!.IsPrivate);
    }

    [Fact]
    public async Task CalculateAsync_InvalidIp_ReturnsValidation()
    {
        var r = await _service.CalculateAsync("999.999.999.999", 24, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task CalculateAsync_InvalidCidr_ReturnsValidation()
    {
        var r = await _service.CalculateAsync("192.168.1.1", 33, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task CalculateAsync_Empty_ReturnsValidation()
    {
        var r = await _service.CalculateAsync("", 24, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}