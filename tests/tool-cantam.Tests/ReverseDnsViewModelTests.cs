using System.Collections.Generic;
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

public class ReverseDnsViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeReverseDnsService _service = new();

    private ReverseDnsViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<ReverseDnsViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.ReverseDns.Status.Ready", vm.StatusText);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasResults);
        Assert.Empty(vm.Aliases);
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
        Assert.Equal("L.ReverseDns.Validation.InvalidIp", vm.StatusText);
    }

    [Fact]
    public async Task Successful_lookup_populates_result_and_reports_success()
    {
        _service.Result = ToolResult<ReverseDnsReport>.Ok(new ReverseDnsReport(
            "8.8.8.8", "IPv4", "dns.google", new List<string> { "google-public-dns-a.google.com" },
            System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal("dns.google", vm.HostName);
        Assert.Equal("IPv4", vm.AddressFamily);
        Assert.Single(vm.Aliases);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
        Assert.StartsWith("L.ReverseDns.Status.Finished", vm.StatusText);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<ReverseDnsReport>.Fail(
            ToolErrorKind.NotFound,
            ReverseDnsErrorCodes.NotFound,
            "No PTR record");

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(ReverseDnsErrorCodes.NotFound, vm.StatusText);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<ReverseDnsReport>.Fail(
            ToolErrorKind.Cancelled,
            ReverseDnsErrorCodes.Cancelled,
            "cancelled");

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyResult_writes_to_clipboard()
    {
        _service.Result = ToolResult<ReverseDnsReport>.Ok(new ReverseDnsReport(
            "8.8.8.8", "IPv4", "dns.google", new List<string>(), System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.CopyResultCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("dns.google", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResult_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.ReverseDns.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<ReverseDnsReport>.Ok(new ReverseDnsReport(
            "8.8.8.8", "IPv4", "dns.google", new List<string>(), System.TimeSpan.FromSeconds(0.3)));

        var vm = CreateViewModel();
        vm.TargetIp = "8.8.8.8";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Empty(vm.Aliases);
        Assert.Equal(string.Empty, vm.HostName);
        Assert.Equal(string.Empty, vm.TargetIp);
        Assert.False(vm.HasResults);
        Assert.Equal("L.ReverseDns.Status.Cleared", vm.StatusText);
    }
}

internal sealed class FakeReverseDnsService : IReverseDnsService
{
    public ToolResult<ReverseDnsReport> Result { get; set; } = ToolResult<ReverseDnsReport>.Ok(
        new ReverseDnsReport("8.8.8.8", "IPv4", "", new List<string>(), System.TimeSpan.Zero));

    public int QueryTimeoutMs => 5000;

    public ToolResult<string> ValidateIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, ReverseDnsErrorCodes.InvalidIp, "empty");
        return ToolResult<string>.Ok(ip.Trim());
    }

    public Task<ToolResult<ReverseDnsReport>> LookupAsync(string? ip, CancellationToken ct = default)
        => Task.FromResult(Result);
}