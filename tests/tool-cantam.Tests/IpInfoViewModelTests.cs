using tool_cantam.Services.ErrorCodes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// IP Info aracýnýn sunum katmaný sözleþmesi: doðrulama, baþarý yolu, hata eþlemesi,
/// kopyalama/kaydetme ve temizleme davranýþý.
/// </summary>
public class IpInfoViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeIpInfoService _service = new();

    private IpInfoViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<IpInfoViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.IpInfo.Status.Ready", vm.StatusText);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasResults);
        Assert.Empty(vm.Country);
    }

    [Fact]
    public async Task Empty_ip_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestTargetFocus += () => focusRequested = true;

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.IpInfo.Validation.InvalidIp", vm.StatusText);
    }

    [Fact]
    public async Task Successful_lookup_populates_fields_and_reports_success()
    {
        _service.Result = ToolResult<IpInfoReport>.Ok(new IpInfoReport(
            "8.8.8.8", "IPv4",
            "United States", "US", "California", "CA", "Mountain View", "94043",
            37.4056, -122.0775, "America/Los_Angeles",
            "Google LLC", "Google Public DNS", "AS15169",
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal("United States", vm.Country);
        Assert.Equal("Mountain View", vm.City);
        Assert.Equal("Google LLC", vm.Isp);
        Assert.Equal("AS15169", vm.Asn);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<IpInfoReport>.Fail(
            ToolErrorKind.NotFound,
            IpInfoErrorCodes.NotFound,
            "not found");

        var vm = CreateViewModel();
        vm.TargetIp = "10.0.0.1";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(IpInfoErrorCodes.NotFound, vm.StatusText);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<IpInfoReport>.Fail(
            ToolErrorKind.Cancelled,
            IpInfoErrorCodes.Cancelled,
            "cancelled");

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task Rate_limit_maps_to_localized_error()
    {
        _service.Result = ToolResult<IpInfoReport>.Fail(
            ToolErrorKind.Validation,
            IpInfoErrorCodes.RateLimited,
            "429");

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(IpInfoErrorCodes.RateLimited, vm.StatusText);
    }

    [Fact]
    public async Task CopyResults_writes_to_clipboard()
    {
        _service.Result = ToolResult<IpInfoReport>.Ok(new IpInfoReport(
            "8.8.8.8", "IPv4",
            "United States", "US", "California", "CA", "Mountain View", "94043",
            37.4056, -122.0775, "America/Los_Angeles",
            "Google LLC", "Google Public DNS", "AS15169",
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("8.8.8.8", _fileExport.ClipboardContent);
        Assert.Contains("Google LLC", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultsCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.IpInfo.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task CopyResults_survives_a_busy_clipboard()
    {
        _service.Result = ToolResult<IpInfoReport>.Ok(new IpInfoReport(
            "8.8.8.8", "IPv4",
            "United States", "US", "California", "CA", "Mountain View", "94043",
            37.4056, -122.0775, "America/Los_Angeles",
            "Google LLC", "Google Public DNS", "AS15169",
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        vm.CopyResultsCommand.Execute(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", vm.StatusText);
    }

    [Fact]
    public async Task SaveResult_writes_through_export_service()
    {
        _service.Result = ToolResult<IpInfoReport>.Ok(new IpInfoReport(
            "8.8.8.8", "IPv4",
            "United States", "US", "California", "CA", "Mountain View", "94043",
            37.4056, -122.0775, "America/Los_Angeles",
            "Google LLC", "Google Public DNS", "AS15169",
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);

        await vm.SaveResultCommand.ExecuteAsync(null);

        Assert.NotNull(_fileExport.ExportedContent);
        Assert.Contains("8.8.8.8", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task SaveResult_reports_cancellation_without_error()
    {
        _service.Result = ToolResult<IpInfoReport>.Ok(new IpInfoReport(
            "8.8.8.8", "IPv4",
            "United States", "US", "California", "CA", "Mountain View", "94043",
            37.4056, -122.0775, "America/Los_Angeles",
            "Google LLC", "Google Public DNS", "AS15169",
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await vm.SaveResultCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, vm.StatusType);
        Assert.Equal("L.IpInfo.Status.SaveCancelled", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<IpInfoReport>.Ok(new IpInfoReport(
            "8.8.8.8", "IPv4",
            "United States", "US", "California", "CA", "Mountain View", "94043",
            37.4056, -122.0775, "America/Los_Angeles",
            "Google LLC", "Google Public DNS", "AS15169",
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.TargetIp);
        Assert.Equal(string.Empty, vm.Country);
        Assert.Equal(string.Empty, vm.City);
        Assert.False(vm.HasResults);
        Assert.Equal("L.IpInfo.Status.Cleared", vm.StatusText);
    }
}

// =====================================================================
// Fake IP Info service
// =====================================================================

internal sealed class FakeIpInfoService : IIpInfoService
{
    public ToolResult<IpInfoReport> Result { get; set; } = ToolResult<IpInfoReport>.Ok(
        new IpInfoReport(
            "8.8.8.8", "IPv4",
            "", "", "", "", "", "",
            0, 0, "",
            "", "", "",
            System.TimeSpan.Zero));

    public int QueryTimeoutMs => 5000;

    public ToolResult<string> ValidateIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, IpInfoErrorCodes.InvalidIp, "empty");
        return ToolResult<string>.Ok(ip.Trim());
    }

    public Task<ToolResult<IpInfoReport>> GetIpInfoAsync(string? ip, CancellationToken ct = default)
        => Task.FromResult(Result);
}