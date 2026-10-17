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

public class DnsViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeDnsService _service = new();

    private DnsViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<DnsViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.Dns.Status.Ready", vm.StatusText);
        Assert.False(vm.IsQuerying);
        Assert.Empty(vm.Records);
    }

    [Fact]
    public async Task Empty_domain_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestTargetFocus += () => focusRequested = true;

        await vm.QueryCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Dns.Validation.InvalidDomain", vm.StatusText);
    }

    [Fact]
    public async Task Successful_lookup_populates_records_and_reports_success()
    {
        _service.Result = ToolResult<DnsLookupReport>.Ok(new DnsLookupReport(
            "example.com",
            System.TimeSpan.FromSeconds(0.5),
            new List<DnsRecordEntry>
            {
                new("A", "example.com", "1.2.3.4"),
                new("MX", "example.com", "mail.example.com", 10)
            }));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.QueryCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Records.Count);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
        Assert.StartsWith("L.Dns.Status.Finished", vm.StatusText);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<DnsLookupReport>.Fail(
            ToolErrorKind.NotFound,
            DnsErrorCodes.DomainNotFound,
            "not found");

        var vm = CreateViewModel();
        vm.TargetDomain = "nonexistent.invalid";

        await vm.QueryCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(DnsErrorCodes.DomainNotFound, vm.StatusText);
        Assert.Empty(vm.Records);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<DnsLookupReport>.Fail(
            ToolErrorKind.Cancelled,
            DnsErrorCodes.Cancelled,
            "cancelled");

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.QueryCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyAll_writes_through_export_service()
    {
        _service.Result = ToolResult<DnsLookupReport>.Ok(new DnsLookupReport(
            "example.com",
            System.TimeSpan.FromSeconds(0.5),
            new List<DnsRecordEntry> { new("A", "example.com", "1.2.3.4") }));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";
        await vm.QueryCommand.ExecuteAsync(null);

        vm.CopyAllCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("1.2.3.4", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyAll_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyAllCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Dns.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<DnsLookupReport>.Ok(new DnsLookupReport(
            "example.com",
            System.TimeSpan.FromSeconds(0.5),
            new List<DnsRecordEntry> { new("A", "example.com", "1.2.3.4") }));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";
        await vm.QueryCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Empty(vm.Records);
        Assert.False(vm.HasResults);
        Assert.Equal("L.Dns.Status.Cleared", vm.StatusText);
    }
}

internal sealed class FakeDnsService : IDnsService
{
    public ToolResult<DnsLookupReport> Result { get; set; } = ToolResult<DnsLookupReport>.Ok(
        new DnsLookupReport("example.com", System.TimeSpan.Zero, new List<DnsRecordEntry>()));

    public int QueryTimeoutMs => 5000;
    public IReadOnlyList<string> SupportedRecordTypes => new[] { "A", "AAAA", "MX", "NS", "TXT", "SOA" };

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, DnsErrorCodes.InvalidDomain, "empty");
        return ToolResult<string>.Ok(domain.Trim().ToLowerInvariant());
    }

    public Task<ToolResult<DnsLookupReport>> LookupAsync(string? domain, CancellationToken ct = default)
        => Task.FromResult(Result);
}