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

public class WhoisViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeWhoisService _service = new();

    private WhoisViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<WhoisViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.Whois.Status.Ready", vm.StatusText);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public async Task Empty_domain_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestTargetFocus += () => focusRequested = true;

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Whois.Validation.InvalidDomain", vm.StatusText);
    }

    [Fact]
    public async Task Successful_lookup_populates_fields_and_reports_success()
    {
        _service.Result = ToolResult<WhoisReport>.Ok(new WhoisReport(
            "example.com", "com", "whois.verisign-grs.com",
            "Example Registrar", "2000-01-01", "2030-01-01", "2020-01-01",
            new List<string> { "ns1.example.com", "ns2.example.com" },
            new List<string> { "clientTransferProhibited" },
            "", "Example Organization", "US", "",
            System.TimeSpan.FromSeconds(0.5), "raw"));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal("Example Registrar", vm.Registrar);
        Assert.Equal("Example Organization", vm.RegistrantOrganization);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<WhoisReport>.Fail(
            ToolErrorKind.NotFound,
            WhoisErrorCodes.NoData,
            "No WHOIS record");

        var vm = CreateViewModel();
        vm.TargetDomain = "nonexistent.invalid";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(WhoisErrorCodes.NoData, vm.StatusText);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<WhoisReport>.Fail(
            ToolErrorKind.Cancelled,
            WhoisErrorCodes.Cancelled,
            "cancelled");

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.LookupCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyResults_writes_summary()
    {
        _service.Result = ToolResult<WhoisReport>.Ok(new WhoisReport(
            "example.com", "com", "whois.verisign-grs.com",
            "Example Registrar", "2000-01-01", "2030-01-01", "2020-01-01",
            new List<string> { "ns1.example.com" },
            new List<string> { "clientTransferProhibited" },
            "", "Example Organization", "US", "",
            System.TimeSpan.FromSeconds(0.5), "raw"));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("Example Registrar", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultsCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Whois.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<WhoisReport>.Ok(new WhoisReport(
            "example.com", "com", "whois.verisign-grs.com",
            "Example Registrar", "", "", "",
            new List<string>(), new List<string>(),
            "", "", "", "",
            System.TimeSpan.FromSeconds(0.5), "raw"));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";
        await vm.LookupCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.TargetDomain);
        Assert.Equal(string.Empty, vm.Registrar);
        Assert.False(vm.HasResults);
        Assert.Equal("L.Whois.Status.Cleared", vm.StatusText);
    }
}

internal sealed class FakeWhoisService : IWhoisService
{
    public ToolResult<WhoisReport> Result { get; set; } = ToolResult<WhoisReport>.Ok(
        new WhoisReport("", "", "", "", "", "", "",
            new List<string>(), new List<string>(),
            "", "", "", "",
            System.TimeSpan.Zero, ""));

    public int QueryTimeoutMs => 10000;

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, WhoisErrorCodes.InvalidDomain, "empty");
        return ToolResult<string>.Ok(domain.Trim().ToLowerInvariant());
    }

    public Task<ToolResult<WhoisReport>> LookupAsync(string? domain, CancellationToken ct = default)
        => Task.FromResult(Result);
}