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

/// <summary>
/// Subdomain Scanner aracının sunum katmanı sözleşmesi: doğrulama, başarı yolu,
/// hata eşlemesi, kopyalama/kaydetme ve temizleme davranışı.
/// </summary>
public class SubdomainViewModelTests
{
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();
    private readonly FakeSubdomainService _service = new();

    private SubdomainViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<SubdomainViewModel>.Instance);

    // ---------------------------------------------------------------------
    // Başlangıç durumu
    // ---------------------------------------------------------------------

    [Fact]
    public void Initial_state_shows_ready_and_limits()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("L.Subdomain.Status.Ready", viewModel.StatusText);
        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.False(viewModel.IsScanning);
        Assert.False(viewModel.HasResults);
        Assert.Equal(0, viewModel.FoundCount);
        Assert.Empty(viewModel.Subdomains);
        Assert.Contains("L.Subdomain.Limits", viewModel.LimitsText);
    }

    // ---------------------------------------------------------------------
    // Boş girdi
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Empty_domain_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestTargetFocus += () => focusRequested = true;

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Subdomain.Validation.InvalidDomain", viewModel.StatusText);
        Assert.False(_service.ScanCalled);
    }

    // ---------------------------------------------------------------------
    // Başarı yolu
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Successful_scan_populates_results_and_reports_success()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com",
            CandidatesChecked: 100,
            FoundCount: 2,
            FailedCount: 98,
            Elapsed: System.TimeSpan.FromSeconds(1.5),
            Found: new List<SubdomainEntry>
            {
                new("api.example.com", "1.2.3.4"),
                new("www.example.com", "5.6.7.8")
            }));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Subdomains.Count);
        Assert.Equal(2, viewModel.FoundCount);
        Assert.True(viewModel.HasResults);
        Assert.Equal(100, viewModel.ProgressPercent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Subdomain.Status.Finished", viewModel.StatusText);
        Assert.False(viewModel.IsScanning);
    }

    [Fact]
    public async Task Successful_scan_with_no_results_reports_info_not_error()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com",
            CandidatesChecked: 100,
            FoundCount: 0,
            FailedCount: 100,
            Elapsed: System.TimeSpan.FromSeconds(1.0),
            Found: new List<SubdomainEntry>()));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Subdomains);
        Assert.Equal(0, viewModel.FoundCount);
        Assert.False(viewModel.HasResults);
        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.StartsWith("L.Subdomain.Status.NoResults", viewModel.StatusText);
    }

    // ---------------------------------------------------------------------
    // Hata eşlemesi
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Service_failure_maps_to_localized_error()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Fail(
            ToolErrorKind.NotFound,
            SubdomainErrorCodes.DomainNotFound,
            "DNS failure");

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "nonexistent.invalid";

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(SubdomainErrorCodes.DomainNotFound, viewModel.StatusText);
        Assert.Empty(viewModel.Subdomains);
    }

    [Fact]
    public async Task Cancelled_scan_is_reported_as_warning_not_error()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Fail(
            ToolErrorKind.Cancelled,
            SubdomainErrorCodes.Cancelled,
            "user cancelled");

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", viewModel.StatusText);
    }

    [Fact]
    public async Task Unexpected_exception_maps_to_unexpected_error()
    {
        _service.ThrowOnScan = new System.InvalidOperationException("boom");

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";

        await viewModel.ScanCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Unexpected", viewModel.StatusText);
        Assert.False(viewModel.IsScanning);
    }

    // ---------------------------------------------------------------------
    // Kopyalama
    // ---------------------------------------------------------------------

    [Fact]
    public async Task CopyResults_writes_hosts_and_ips_to_clipboard()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com", 10, 1, 9, System.TimeSpan.FromSeconds(1),
            new List<SubdomainEntry> { new("www.example.com", "1.2.3.4") }));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";
        await viewModel.ScanCommand.ExecuteAsync(null);

        viewModel.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("www.example.com", _fileExport.ClipboardContent);
        Assert.Contains("1.2.3.4", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Subdomain.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyResults_survives_a_busy_clipboard()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com", 10, 1, 9, System.TimeSpan.FromSeconds(1),
            new List<SubdomainEntry> { new("www.example.com", "1.2.3.4") }));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";
        await viewModel.ScanCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    // ---------------------------------------------------------------------
    // Kaydetme
    // ---------------------------------------------------------------------

    [Fact]
    public async Task SaveResults_writes_through_export_service()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com", 10, 1, 9, System.TimeSpan.FromSeconds(1),
            new List<SubdomainEntry> { new("www.example.com", "1.2.3.4") }));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";
        await viewModel.ScanCommand.ExecuteAsync(null);

        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.NotNull(_fileExport.ExportedContent);
        Assert.Contains("www.example.com", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveResults_reports_cancellation_without_error()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com", 10, 1, 9, System.TimeSpan.FromSeconds(1),
            new List<SubdomainEntry> { new("www.example.com", "1.2.3.4") }));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";
        await viewModel.ScanCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Subdomain.Status.SaveCancelled", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveResults_without_results_warns()
    {
        var viewModel = CreateViewModel();

        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.Null(_fileExport.ExportedContent);
        Assert.Equal("L.Subdomain.Validation.NothingToSave", viewModel.StatusText);
    }

    // ---------------------------------------------------------------------
    // Temizleme
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Clear_resets_state()
    {
        _service.Result = ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            "example.com", 10, 1, 9, System.TimeSpan.FromSeconds(1),
            new List<SubdomainEntry> { new("www.example.com", "1.2.3.4") }));

        var viewModel = CreateViewModel();
        viewModel.TargetDomain = "example.com";
        await viewModel.ScanCommand.ExecuteAsync(null);

        viewModel.ClearCommand.Execute(null);

        Assert.Empty(viewModel.Subdomains);
        Assert.Equal(0, viewModel.FoundCount);
        Assert.False(viewModel.HasResults);
        Assert.Equal(string.Empty, viewModel.SummaryText);
        Assert.Equal(0, viewModel.ProgressPercent);
        Assert.Equal("L.Subdomain.Status.Cleared", viewModel.StatusText);
    }
}

// =====================================================================
// Test doubles (Fake Servisler)
// =====================================================================

internal sealed class FakeSubdomainService : ISubdomainService
{
    public ToolResult<SubdomainScanReport> Result { get; set; } = ToolResult<SubdomainScanReport>.Ok(
        new SubdomainScanReport("example.com", 0, 0, 0, System.TimeSpan.Zero, new List<SubdomainEntry>()));

    public System.Exception? ThrowOnScan { get; set; }
    public bool ScanCalled { get; private set; }

    public int MaxCandidatesPerScan => 100;
    public int MaxConcurrency => 10;
    public int QueryTimeoutMs => 1000;

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SubdomainErrorCodes.InvalidDomain, "empty");
        return ToolResult<string>.Ok(domain.Trim().ToLowerInvariant());
    }

    public async Task<ToolResult<SubdomainScanReport>> ScanAsync(
        string? domain,
        IProgress<SubdomainScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ScanCalled = true;

        if (ThrowOnScan is not null)
            throw ThrowOnScan;

        await Task.CompletedTask;
        return Result;
    }
}