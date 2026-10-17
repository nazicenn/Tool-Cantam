using System.Net;
using System.Net.Sockets;
using tool_cantam.Core.Models;
using tool_cantam.Core.Net;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Port tarayıcının sözleşmesini kilitler: gerçek bir yerel dinleyici üzerinde açık/kapalı port
/// ayrımı, hedef çözümleme, sınırlar, iptal ve ilerleme bildirimi.
/// </summary>
public class PortScannerServiceTests
{
    private readonly PortScannerService _service = new();

    [Fact]
    public async Task An_open_port_is_detected()
    {
        using var listener = new LocalListener();
        int port = listener.Port;

        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "127.0.0.1", port, port, 1000, null, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, $"{result.ErrorKind}/{result.ErrorCode}: {result.TechnicalDetail}");

        PortScanReport report = result.Value!;
        PortScanEntry entry = Assert.Single(report.OpenPorts);
        Assert.Equal(port, entry.Port);
        Assert.Equal(1, report.OpenCount);
        Assert.Equal(1, report.Scanned);
        Assert.True(report.Elapsed >= TimeSpan.Zero);
    }

    [Fact]
    public async Task A_closed_port_is_not_reported_as_open()
    {
        int port;

        using (var listener = new LocalListener())
            port = listener.Port;

        // Dinleyici kapatıldı: bağlantı reddedilmeli.
        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "127.0.0.1", port, port, 500, null, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.OpenPorts);
        Assert.Equal(0, result.Value.OpenCount);
        Assert.Equal(1, result.Value.Scanned);
    }

    [Fact]
    public async Task A_host_name_is_resolved_once_and_then_scanned_by_address()
    {
        using var listener = new LocalListener();

        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "localhost", listener.Port, listener.Port, 1000, null, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, $"{result.ErrorKind}/{result.ErrorCode}: {result.TechnicalDetail}");
        Assert.Equal("localhost", result.Value!.Target);
        Assert.Single(result.Value.OpenPorts);
        Assert.True(
            result.Value.Address.Equals(IPAddress.Loopback) || result.Value.Address.Equals(IPAddress.IPv6Loopback),
            $"Beklenen yerel adres, gelen: {result.Value.Address}");
    }

    [Fact]
    public async Task Progress_is_reported_for_the_scanned_range()
    {
        using var listener = new LocalListener();
        var reports = new List<PortScanProgress>();
        var progress = new Progress<PortScanProgress>(update => reports.Add(update));

        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "127.0.0.1", listener.Port, listener.Port + 3, 500, progress, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Scanned);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 70000)]
    [InlineData(100, 50)]
    public async Task An_invalid_range_is_rejected(int startPort, int endPort)
    {
        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "127.0.0.1", startPort, endPort, 500, null, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(PortScanErrorCodes.InvalidRange, result.ErrorCode);
    }

    [Fact]
    public async Task A_range_above_the_limit_is_rejected()
    {
        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "127.0.0.1", 1, PortScannerService.MaxPortsPerScanLimit + 1, 500, null, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(PortScanErrorCodes.RangeTooLarge, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bu bir host degil!")]
    public async Task An_invalid_target_is_rejected(string target)
    {
        ToolResult<PortScanReport> result = await _service.ScanAsync(
            target, 80, 80, 500, null, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(PortScanErrorCodes.InvalidTarget, result.ErrorCode);
    }

    [Fact]
    public async Task An_unresolvable_host_is_reported_as_not_found()
    {
        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "no-such-host.invalid", 80, 80, 500, null, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(PortScanErrorCodes.TargetNotFound, result.ErrorCode);
        Assert.Equal(ToolErrorKind.NotFound, result.ErrorKind);
    }

    [Fact]
    public async Task A_cancelled_token_stops_the_scan()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        ToolResult<PortScanReport> result = await _service.ScanAsync(
            "127.0.0.1", 1, 100, 500, null, cancellation.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, result.ErrorKind);
    }

    [Fact]
    public void Limits_are_exposed_to_the_view()
    {
        Assert.True(_service.MaxPortsPerScan > 0);
        Assert.True(_service.MaxConcurrency > 0);
        Assert.True(_service.MaxTimeoutMs > 0);
    }

    [Theory]
    [InlineData(22, "ssh")]
    [InlineData(80, "http")]
    [InlineData(443, "https")]
    [InlineData(3306, "mysql")]
    [InlineData(5432, "postgresql")]
    public void Well_known_ports_carry_a_service_name(int port, string expected)
    {
        Assert.Equal(expected, WellKnownPorts.GetServiceName(port));
    }

    [Fact]
    public void Unknown_ports_have_no_service_name()
    {
        Assert.Null(WellKnownPorts.GetServiceName(64999));
    }

    /// <summary>Rastgele bir portta yerel TCP dinleyicisi açar.</summary>
    private sealed class LocalListener : IDisposable
    {
        private readonly TcpListener _listener;

        public LocalListener()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public int Port { get; }

        public void Dispose() => _listener.Stop();
    }
}
