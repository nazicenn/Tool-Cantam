using tool_cantam.Services.ErrorCodes;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Port tarayıcının sunum katmanı sözleşmesi: girdi doğrulama, tarama sınırı, sonuç listesi,
/// özet/ilerleme metinleri, iptal ve pano/kaydetme davranışı. Ağa çıkılmaz.
/// </summary>
public class PortScannerViewModelTests
{
    private readonly FakePortScannerService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private PortScannerViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<PortScannerViewModel>.Instance);

    [Fact]
    public void Default_state_shows_the_limits_and_a_safe_range()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("127.0.0.1", viewModel.TargetHost);
        Assert.Equal(1, viewModel.StartPort);
        Assert.Equal(1024, viewModel.EndPort);
        Assert.Equal(1000, viewModel.SelectedTimeoutMs);
        Assert.Equal(4096, viewModel.MaxPortsPerScan);
        Assert.StartsWith("L.PortScan.Limits", viewModel.LimitsText);
        Assert.StartsWith("L.PortScan.Progress.Initial", viewModel.ProgressText);
        Assert.Equal("L.PortScan.Status.Ready", viewModel.StatusText);
        Assert.Empty(viewModel.OpenPorts);
    }

    [Fact]
    public async Task Missing_target_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        viewModel.TargetHost = "   ";
        bool focusRequested = false;
        viewModel.RequestTargetFocus += () => focusRequested = true;

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.PortScan.Validation.InvalidTarget", viewModel.StatusText);
        Assert.Equal(0, _service.ScanCount);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 50)]
    [InlineData(1, 70000)]
    public async Task Invalid_range_is_rejected_before_scanning(int startPort, int endPort)
    {
        var viewModel = CreateViewModel();
        viewModel.StartPort = startPort;
        viewModel.EndPort = endPort;

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.PortScan.Validation.InvalidRange", viewModel.StatusText);
        Assert.Equal(0, _service.ScanCount);
    }

    [Fact]
    public async Task A_range_above_the_limit_is_rejected_with_the_limit_in_the_message()
    {
        var viewModel = CreateViewModel();
        viewModel.StartPort = 1;
        viewModel.EndPort = 5000;

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.StartsWith("L.PortScan.Validation.RangeTooLarge|5000|4096", viewModel.StatusText);
        Assert.Equal(0, _service.ScanCount);
    }

    [Fact]
    public async Task A_successful_scan_fills_the_result_list()
    {
        var viewModel = CreateViewModel();
        viewModel.TargetHost = "10.0.0.5";
        viewModel.StartPort = 20;
        viewModel.EndPort = 100;
        viewModel.SelectedTimeoutMs = 2000;
        _service.NextResult = ToolResult<PortScanReport>.Ok(
            FakePortScannerService.Report("10.0.0.5", 20, 100, 22, 80));

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.OpenPorts.Count);
        Assert.Equal(22, viewModel.OpenPorts[0].Port);
        Assert.Equal("ssh", viewModel.OpenPorts[0].ServiceText);
        Assert.Equal(80, viewModel.OpenPorts[1].Port);
        Assert.True(viewModel.HasResults);
        Assert.Equal(100, viewModel.ProgressPercent);

        Assert.Equal(20, _service.LastStartPort);
        Assert.Equal(100, _service.LastEndPort);
        Assert.Equal(2000, _service.LastTimeoutMs);

        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.PortScan.Status.Summary", viewModel.SummaryText);
        Assert.False(viewModel.IsScanning);
    }

    [Fact]
    public async Task A_scan_without_open_ports_is_reported_as_information()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Ok(
            FakePortScannerService.Report("127.0.0.1", 1, 1024));

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.OpenPorts);
        Assert.False(viewModel.HasResults);
        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.StartsWith("L.PortScan.Status.NoOpenPorts", viewModel.StatusText);
    }

    [Fact]
    public async Task A_resolution_failure_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Fail(
            ToolErrorKind.NotFound, PortScanErrorCodes.TargetNotFound, "dns failure");

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(PortScanErrorCodes.TargetNotFound, viewModel.StatusText);
        Assert.Empty(viewModel.OpenPorts);
        Assert.False(viewModel.IsScanning);
    }

    [Fact]
    public async Task A_cancelled_scan_is_reported_as_a_warning()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Fail(
            ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "cancelled");

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyResults_writes_a_port_and_service_table()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Ok(
            FakePortScannerService.Report("127.0.0.1", 1, 1024, 22, 443));

        await viewModel.ScanCommand.ExecuteAsync(null);
        viewModel.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("L.PortScan.Column.Port", _fileExport.ClipboardContent);
        Assert.Contains("22\tssh", _fileExport.ClipboardContent);
        Assert.Contains("443\thttps", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.PortScan.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyResults_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Ok(
            FakePortScannerService.Report("127.0.0.1", 1, 1024, 22));

        await viewModel.ScanCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveResults_writes_the_table()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Ok(
            FakePortScannerService.Report("127.0.0.1", 1, 1024, 22));

        await viewModel.ScanCommand.ExecuteAsync(null);
        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.NotNull(_fileExport.ExportedContent);
        Assert.Contains("22\tssh", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveResults_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();

        // Sonuç yok: kaydetme uyarı verir.
        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.Equal("L.PortScan.Validation.NothingToSave", viewModel.StatusText);
    }

    [Fact]
    public async Task Clear_resets_results_and_progress()
    {
        var viewModel = CreateViewModel();
        _service.NextResult = ToolResult<PortScanReport>.Ok(
            FakePortScannerService.Report("127.0.0.1", 1, 1024, 22));

        await viewModel.ScanCommand.ExecuteAsync(null);
        viewModel.ClearCommand.Execute(null);

        Assert.Empty(viewModel.OpenPorts);
        Assert.False(viewModel.HasResults);
        Assert.Equal(string.Empty, viewModel.SummaryText);
        Assert.Equal(0, viewModel.ProgressPercent);
        Assert.Equal("L.PortScan.Status.Cleared", viewModel.StatusText);
    }
}