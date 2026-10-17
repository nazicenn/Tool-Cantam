using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

public class MacLookupServiceTests
{
    private readonly MacLookupService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeMac_reports_empty(string? mac)
    {
        var r = _service.NormalizeMac(mac);
        Assert.False(r.IsSuccess);
        Assert.Equal(MacLookupErrorCodes.InvalidMac, r.ErrorCode);
    }

    [Theory]
    [InlineData("00:1A:2B:3C:4D:5E", "001A2B3C4D5E")]
    [InlineData("00-1A-2B-3C-4D-5E", "001A2B3C4D5E")]
    [InlineData("001A2B3C4D5E", "001A2B3C4D5E")]
    [InlineData("001a.2b3c.4d5e", "001A2B3C4D5E")]
    [InlineData("  00:1A:2B:3C:4D:5E  ", "001A2B3C4D5E")]
    public void NormalizeMac_accepts_valid_formats(string input, string expected)
    {
        var r = _service.NormalizeMac(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("00:1A:2B")]
    [InlineData("00:1A:2B:3C:4D:5E:FF")]
    [InlineData("GG:1A:2B:3C:4D:5E")]
    [InlineData("001A2B3C4D5")]
    public void NormalizeMac_rejects_malformed(string input)
    {
        var r = _service.NormalizeMac(input);
        Assert.False(r.IsSuccess);
        Assert.Equal(MacLookupErrorCodes.InvalidMac, r.ErrorCode);
    }

    [Fact]
    public async Task LookupVendorAsync_rejects_invalid_mac()
    {
        var r = await _service.LookupVendorAsync("bad mac", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(MacLookupErrorCodes.InvalidMac, r.ErrorCode);
    }

    [Fact]
    public async Task LookupVendorAsync_detects_multicast_without_network_call()
    {
        // 01:xx → multicast (LSB = 1)
        var r = await _service.LookupVendorAsync("01:00:5E:00:00:01", TestContext.Current.CancellationToken);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.IsMulticast);
        Assert.Equal("Multicast Address", r.Value.Vendor);
    }

    [Fact]
    public async Task LookupVendorAsync_detects_locally_administered()
    {
        // 02:xx → locally administered (bit 1 = 1)
        var r = await _service.LookupVendorAsync("02:00:00:00:00:01", TestContext.Current.CancellationToken);

        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.IsLocallyAdministered);
        Assert.Equal("Locally Administered Address", r.Value.Vendor);
    }

    [Fact]
    public async Task LookupVendorAsync_maps_429_to_RateLimited()
    {
        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)));
        var service = new MacLookupService(new HttpClient(handler));

        var r = await service.LookupVendorAsync("00:1A:2B:3C:4D:5E", TestContext.Current.CancellationToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(MacLookupErrorCodes.RateLimited, r.ErrorCode);
    }

    [Fact]
    public async Task LookupVendorAsync_maps_500_to_NetworkError()
    {
        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var service = new MacLookupService(new HttpClient(handler));

        var r = await service.LookupVendorAsync("00:1A:2B:3C:4D:5E", TestContext.Current.CancellationToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(MacLookupErrorCodes.NetworkError, r.ErrorCode);
    }

    [Fact]
    public async Task LookupVendorAsync_maps_empty_company_to_NotFound()
    {
        string body = "{\"company\":\"\",\"address\":\"\",\"country\":\"\"}";
        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            }));
        var service = new MacLookupService(new HttpClient(handler));

        var r = await service.LookupVendorAsync("00:1A:2B:3C:4D:5E", TestContext.Current.CancellationToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(MacLookupErrorCodes.VendorNotFound, r.ErrorCode);
    }

    [Fact]
    public async Task LookupVendorAsync_parses_success_body()
    {
        string body = "{\"company\":\"Intel Corporate\",\"address\":\"2200 Mission College Blvd\",\"country\":\"US\"}";
        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            }));
        var service = new MacLookupService(new HttpClient(handler));

        var r = await service.LookupVendorAsync("00:1A:2B:3C:4D:5E", TestContext.Current.CancellationToken);

        Assert.True(r.IsSuccess);
        Assert.Equal("Intel Corporate", r.Value!.Vendor);
        Assert.Equal("US", r.Value.Country);
        Assert.Equal("001A2B", r.Value.Oui);
    }

    [Fact]
    public async Task LookupVendorAsync_honours_cancellation()
    {
        var handler = new FakeHttpHandler(async (_, ct) =>
        {
            await Task.Delay(2000, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new MacLookupService(new HttpClient(handler));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cts.Cancel();

        var r = await service.LookupVendorAsync("00:1A:2B:3C:4D:5E", cts.Token);

        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, r.ErrorKind);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.QueryTimeoutMs > 0);
        Assert.True(_service.QueryTimeoutMs <= 15000);
    }
}

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly System.Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHttpHandler(System.Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request, cancellationToken);
}