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

public class PingViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakePingService _service = new();

    private PingViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<PingViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.Ping.Status.Ready", vm.StatusText);
        Assert.False(vm.IsPinging);
        Assert.False(vm.HasResults);
        Assert.Empty(vm.Results);
    }

    [Fact]
    public async Task Empty_target_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        vm.TargetHost = "   ";
        bool focusRequested = false;
        vm.RequestTargetFocus += () => focusRequested = true;

        await vm.PingCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Ping.Validation.InvalidTarget", vm.StatusText);
    }

    [Fact]
    public async Task Successful_ping_populates_rows_and_statistics()
    {
        _service.Result = ToolResult<PingReport>.Ok(new PingReport(
            "google.com", "142.250.1.1",
            Sent: 4, Received: 4, Lost: 0, PacketLossPercent: 0,
            MinMs: 10, MaxMs: 20, AvgMs: 15,
            Attempts: new List<PingAttempt>
            {
                new(1, "Success", 10, "142.250.1.1"),
                new(2, "Success", 12, "142.250.1.1"),
                new(3, "Success", 18, "142.250.1.1"),
                new(4, "Success", 20, "142.250.1.1")
            },
            Elapsed: System.TimeSpan.FromSeconds(1.2)));

        var vm = CreateViewModel();
        vm.TargetHost = "google.com";

        await vm.PingCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.Results.Count);
        Assert.Equal(4, vm.Sent);
        Assert.Equal(4, vm.Received);
        Assert.Equal(0, vm.Lost);
        Assert.Equal(0, vm.PacketLoss);
        Assert.Equal("10 ms", vm.MinText);
        Assert.Equal("15 ms", vm.AvgText);
        Assert.Equal("20 ms", vm.MaxText);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Partial_loss_is_reported()
    {
        _service.Result = ToolResult<PingReport>.Ok(new PingReport(
            "test.com", "1.2.3.4",
            Sent: 4, Received: 2, Lost: 2, PacketLossPercent: 50,
            MinMs: 10, MaxMs: 20, AvgMs: 15,
            Attempts: new List<PingAttempt>
            {
                new(1, "Success", 10, "1.2.3.4"),
                new(2, "TimedOut", null, ""),
                new(3, "Success", 20, "1.2.3.4"),
                new(4, "TimedOut", null, "")
            },
            Elapsed: System.TimeSpan.FromSeconds(2)));

        var vm = CreateViewModel();
        vm.TargetHost = "test.com";

        await vm.PingCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.Results.Count);
        Assert.Equal(2, vm.Received);
        Assert.Equal(2, vm.Lost);
        Assert.Equal(50, vm.PacketLoss);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Zero_replies_reports_warning()
    {
        _service.Result = ToolResult<PingReport>.Ok(new PingReport(
            "down.com", "1.2.3.4",
            Sent: 4, Received: 0, Lost: 4, PacketLossPercent: 100,
            MinMs: null, MaxMs: null, AvgMs: null,
            Attempts: new List<PingAttempt>
            {
                new(1, "TimedOut", null, ""),
                new(2, "TimedOut", null, ""),
                new(3, "TimedOut", null, ""),
                new(4, "TimedOut", null, "")
            },
            Elapsed: System.TimeSpan.FromSeconds(4)));

        var vm = CreateViewModel();
        vm.TargetHost = "down.com";

        await vm.PingCommand.ExecuteAsync(null);

        Assert.Equal(4, vm.Results.Count);
        Assert.Equal(0, vm.Received);
        Assert.Equal(4, vm.Lost);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.StartsWith("L.Ping.Status.NoReply", vm.StatusText);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<PingReport>.Fail(
            ToolErrorKind.NotFound, PingErrorCodes.TargetNotFound, "dns failure");

        var vm = CreateViewModel();
        vm.TargetHost = "nonexistent.invalid";

        await vm.PingCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(PingErrorCodes.TargetNotFound, vm.StatusText);
        Assert.Empty(vm.Results);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<PingReport>.Fail(
            ToolErrorKind.Cancelled, PingErrorCodes.Cancelled, "cancelled");

        var vm = CreateViewModel();
        vm.TargetHost = "google.com";

        await vm.PingCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyResults_writes_table_to_clipboard()
    {
        _service.Result = ToolResult<PingReport>.Ok(new PingReport(
            "google.com", "142.250.1.1",
            Sent: 2, Received: 2, Lost: 0, PacketLossPercent: 0,
            MinMs: 10, MaxMs: 15, AvgMs: 12,
            Attempts: new List<PingAttempt>
            {
                new(1, "Success", 10, "142.250.1.1"),
                new(2, "Success", 15, "142.250.1.1")
            },
            Elapsed: System.TimeSpan.FromSeconds(0.5)));

        var vm = CreateViewModel();
        vm.TargetHost = "google.com";
        await vm.PingCommand.ExecuteAsync(null);

        vm.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("google.com", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultsCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Ping.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<PingReport>.Ok(new PingReport(
            "google.com", "1.2.3.4",
            Sent: 1, Received: 1, Lost: 0, PacketLossPercent: 0,
            MinMs: 10, MaxMs: 10, AvgMs: 10,
            Attempts: new List<PingAttempt> { new(1, "Success", 10, "1.2.3.4") },
            Elapsed: System.TimeSpan.FromSeconds(0.5)));

        var vm = CreateViewModel();
        vm.TargetHost = "google.com";
        await vm.PingCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Empty(vm.Results);
        Assert.False(vm.HasResults);
        Assert.Equal(0, vm.Sent);
        Assert.Equal("L.Ping.Status.Cleared", vm.StatusText);
    }
}

internal sealed class FakePingService : IPingService
{
    public ToolResult<PingReport> Result { get; set; } = ToolResult<PingReport>.Ok(
        new PingReport("", "", 0, 0, 0, 0, null, null, null,
            new List<PingAttempt>(), System.TimeSpan.Zero));

    public int MaxCount => 20;
    public int MinTimeoutMs => 100;
    public int MaxTimeoutMs => 10000;

    public ToolResult<string> ValidateTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, PingErrorCodes.InvalidTarget, "empty");
        return ToolResult<string>.Ok(target.Trim());
    }

    public Task<ToolResult<PingReport>> PingAsync(
        string? target, int count, int timeoutMs,
        IProgress<PingProgress>? progress = null, CancellationToken ct = default)
        => Task.FromResult(Result);
}