using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

public class MacLookupViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeMacLookupService _service = new();

    private MacLookupViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<MacLookupViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.MacLookup.Status.Ready", vm.StatusText);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public void Live_format_preview_works_on_12_hex_digits()
    {
        var vm = CreateViewModel();
        vm.MacAddress = "001a2b3c4d5e";
        Assert.Equal("00:1A:2B:3C:4D:5E", vm.FormattedMac);
    }

    [Fact]
    public void Live_format_clears_on_incomplete_input()
    {
        var vm = CreateViewModel();
        vm.MacAddress = "001A";
        Assert.Equal(string.Empty, vm.FormattedMac);
    }

    [Fact]
    public async Task Empty_mac_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestTargetFocus += () => focusRequested = true;

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.MacLookup.Validation.InvalidMac", vm.StatusText);
    }

    [Fact]
    public async Task Successful_lookup_populates_fields_and_reports_success()
    {
        _service.Result = ToolResult<MacLookupReport>.Ok(new MacLookupReport(
            "001A2B3C4D5E", "00:1A:2B:3C:4D:5E", "001A2B",
            "Intel Corporate", "2200 Mission College Blvd", "US",
            false, false,
            System.TimeSpan.FromSeconds(0.4)));

        var vm = CreateViewModel();
        vm.MacAddress = "00:1A:2B:3C:4D:5E";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal("Intel Corporate", vm.Vendor);
        Assert.Equal("US", vm.Country);
        Assert.Equal("001A2B", vm.Oui);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<MacLookupReport>.Fail(
            ToolErrorKind.NotFound,
            MacLookupErrorCodes.VendorNotFound,
            "not found");

        var vm = CreateViewModel();
        vm.MacAddress = "00:1A:2B:3C:4D:5E";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(MacLookupErrorCodes.VendorNotFound, vm.StatusText);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<MacLookupReport>.Fail(
            ToolErrorKind.Cancelled,
            MacLookupErrorCodes.Cancelled,
            "cancelled");

        var vm = CreateViewModel();
        vm.MacAddress = "00:1A:2B:3C:4D:5E";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyResult_writes_to_clipboard()
    {
        _service.Result = ToolResult<MacLookupReport>.Ok(new MacLookupReport(
            "001A2B3C4D5E", "00:1A:2B:3C:4D:5E", "001A2B",
            "Intel Corporate", "", "US",
            false, false,
            System.TimeSpan.FromSeconds(0.4)));

        var vm = CreateViewModel();
        vm.MacAddress = "00:1A:2B:3C:4D:5E";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.CopyResultCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("Intel Corporate", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResult_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.MacLookup.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<MacLookupReport>.Ok(new MacLookupReport(
            "001A2B3C4D5E", "00:1A:2B:3C:4D:5E", "001A2B",
            "Intel Corporate", "", "US",
            false, false,
            System.TimeSpan.FromSeconds(0.4)));

        var vm = CreateViewModel();
        vm.MacAddress = "00:1A:2B:3C:4D:5E";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.MacAddress);
        Assert.Equal(string.Empty, vm.Vendor);
        Assert.False(vm.HasResults);
        Assert.Equal("L.MacLookup.Status.Cleared", vm.StatusText);
    }
}

internal sealed class FakeMacLookupService : IMacLookupService
{
    public ToolResult<MacLookupReport> Result { get; set; } = ToolResult<MacLookupReport>.Ok(
        new MacLookupReport("", "", "", "", "", "", false, false, System.TimeSpan.Zero));

    public int QueryTimeoutMs => 8000;

    public ToolResult<string> NormalizeMac(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, MacLookupErrorCodes.InvalidMac, "empty");
        return ToolResult<string>.Ok(mac.Replace(":", "").Trim().ToUpperInvariant());
    }

    public Task<ToolResult<MacLookupReport>> LookupVendorAsync(string? mac, CancellationToken ct = default)
        => Task.FromResult(Result);
}