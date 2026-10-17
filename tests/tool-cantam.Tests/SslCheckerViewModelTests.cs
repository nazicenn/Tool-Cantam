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

public class SslCheckerViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeSslCheckerService _service = new();

    private SslCheckerViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<SslCheckerViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.SslChecker.Status.Ready", vm.StatusText);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasResults);
    }

    [Fact]
    public async Task Empty_domain_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestTargetFocus += () => focusRequested = true;

        await vm.CheckCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.SslChecker.Validation.InvalidDomain", vm.StatusText);
    }

    [Fact]
    public async Task Successful_check_populates_fields()
    {
        _service.Result = ToolResult<SslCertificateReport>.Ok(new SslCertificateReport(
            "example.com", "CN=example.com", "CN=DigiCert", "example.com",
            System.DateTime.Now.AddYears(-1), System.DateTime.Now.AddMonths(6), 180,
            "ABC123", "SHA256", "Tls13",
            false, false, true, true, true, "Valid",
            new List<string>(), System.TimeSpan.FromSeconds(0.5)));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.CheckCommand.ExecuteAsync(null);

        Assert.Equal("Valid", vm.CertificateStatus);
        Assert.True(vm.IsValid);
        Assert.Equal(180, vm.RemainingDays);
        Assert.True(vm.HasResults);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Expired_certificate_reports_warning()
    {
        _service.Result = ToolResult<SslCertificateReport>.Ok(new SslCertificateReport(
            "example.com", "CN=example.com", "CN=DigiCert", "example.com",
            System.DateTime.Now.AddYears(-2), System.DateTime.Now.AddDays(-10), -10,
            "ABC123", "SHA256", "Tls13",
            true, false, true, true, false, "Expired",
            new List<string>(), System.TimeSpan.FromSeconds(0.5)));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.CheckCommand.ExecuteAsync(null);

        Assert.Equal("Expired", vm.CertificateStatus);
        Assert.False(vm.IsValid);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
    }

    [Fact]
    public async Task Failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<SslCertificateReport>.Fail(
            ToolErrorKind.NotFound, SslCheckerErrorCodes.ConnectionFailed, "refused");

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.CheckCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(SslCheckerErrorCodes.ConnectionFailed, vm.StatusText);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.Result = ToolResult<SslCertificateReport>.Fail(
            ToolErrorKind.Cancelled, SslCheckerErrorCodes.Cancelled, "cancelled");

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";

        await vm.CheckCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyResult_writes_to_clipboard()
    {
        _service.Result = ToolResult<SslCertificateReport>.Ok(new SslCertificateReport(
            "example.com", "CN=example.com", "CN=DigiCert", "example.com",
            System.DateTime.Now.AddYears(-1), System.DateTime.Now.AddMonths(6), 180,
            "ABC123", "SHA256", "Tls13",
            false, false, true, true, true, "Valid",
            new List<string>(), System.TimeSpan.FromSeconds(0.5)));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";
        await vm.CheckCommand.ExecuteAsync(null);

        vm.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("Valid", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultsCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.SslChecker.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<SslCertificateReport>.Ok(new SslCertificateReport(
            "example.com", "CN=example.com", "CN=DigiCert", "example.com",
            System.DateTime.Now.AddYears(-1), System.DateTime.Now.AddMonths(6), 180,
            "ABC123", "SHA256", "Tls13",
            false, false, true, true, true, "Valid",
            new List<string>(), System.TimeSpan.FromSeconds(0.5)));

        var vm = CreateViewModel();
        vm.TargetDomain = "example.com";
        await vm.CheckCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.TargetDomain);
        Assert.Equal(string.Empty, vm.CertificateStatus);
        Assert.False(vm.HasResults);
        Assert.Equal("L.SslChecker.Status.Cleared", vm.StatusText);
    }
}

internal sealed class FakeSslCheckerService : ISslCheckerService
{
    public ToolResult<SslCertificateReport> Result { get; set; } = ToolResult<SslCertificateReport>.Ok(
        new SslCertificateReport("", "", "", "",
            System.DateTime.MinValue, System.DateTime.MinValue, 0,
            "", "", "", false, false, false, false, false, "",
            new List<string>(), System.TimeSpan.Zero));

    public int QueryTimeoutMs => 10000;

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SslCheckerErrorCodes.InvalidDomain, "empty");
        return ToolResult<string>.Ok(domain.Trim().ToLowerInvariant());
    }

    public Task<ToolResult<SslCertificateReport>> CheckAsync(string? domain, CancellationToken ct = default)
        => Task.FromResult(Result);
}