using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Text Counter aracının sunum katmanı sözleşmesi: otomatik güncelleme, hata eşlemesi,
/// kopyalama/kaydetme ve temizleme davranışı.
/// </summary>
public class TextCounterViewModelTests
{
    private readonly FakeTextCounterService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private TextCounterViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<TextCounterViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.TextCounter.Status.Ready", vm.StatusText);
        Assert.Equal(0, vm.CharacterCount);
        Assert.Equal(0, vm.WordCount);
        Assert.Empty(vm.TopWords);
    }

    [Fact]
    public void Typing_updates_counts_automatically()
    {
        _service.NextResult = ToolResult<TextCounterReport>.Ok(new TextCounterReport(
            11, 10, 2, 1, 1, 1,
            System.TimeSpan.FromSeconds(1),
            System.TimeSpan.FromSeconds(2),
            new[] { new WordFrequency("hello", 1) }));

        var vm = CreateViewModel();
        vm.InputText = "hello world";

        Assert.Equal(11, vm.CharacterCount);
        Assert.Equal(10, vm.CharacterCountNoSpaces);
        Assert.Equal(2, vm.WordCount);
        Assert.Equal(1, vm.SentenceCount);
        Assert.Single(vm.TopWords);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void Clearing_input_resets_counts()
    {
        _service.NextResult = ToolResult<TextCounterReport>.Ok(new TextCounterReport(
            5, 5, 1, 1, 1, 1,
            System.TimeSpan.Zero, System.TimeSpan.Zero,
            System.Array.Empty<WordFrequency>()));

        var vm = CreateViewModel();
        vm.InputText = "hello";

        vm.InputText = string.Empty;

        Assert.Equal(0, vm.CharacterCount);
        Assert.Equal(0, vm.WordCount);
        Assert.Empty(vm.TopWords);
        Assert.Equal("L.TextCounter.Status.Ready", vm.StatusText);
    }

    [Fact]
    public void Service_error_maps_to_localized_message()
    {
        _service.NextResult = ToolResult<TextCounterReport>.Fail(
            ToolErrorKind.Validation,
            TextCounterErrorCodes.InvalidUnicode,
            "bad surrogate");

        var vm = CreateViewModel();
        vm.InputText = "abc";

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(TextCounterErrorCodes.InvalidUnicode, vm.StatusText);
        Assert.Equal(0, vm.CharacterCount);
    }

    [Fact]
    public void CopyResults_writes_report_to_clipboard()
    {
        _service.NextResult = ToolResult<TextCounterReport>.Ok(new TextCounterReport(
            11, 10, 2, 1, 1, 1,
            System.TimeSpan.FromSeconds(1),
            System.TimeSpan.FromSeconds(2),
            new[] { new WordFrequency("hello", 1) }));

        var vm = CreateViewModel();
        vm.InputText = "hello world";

        vm.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("Characters", _fileExport.ClipboardContent);
        Assert.Contains("hello", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyResults_without_input_warns()
    {
        var vm = CreateViewModel();
        vm.CopyResultsCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.TextCounter.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task SaveResults_writes_through_export_service()
    {
        _service.NextResult = ToolResult<TextCounterReport>.Ok(new TextCounterReport(
            11, 10, 2, 1, 1, 1,
            System.TimeSpan.Zero, System.TimeSpan.Zero,
            System.Array.Empty<WordFrequency>()));

        var vm = CreateViewModel();
        vm.InputText = "hello world";

        await vm.SaveResultsCommand.ExecuteAsync(null);

        Assert.NotNull(_fileExport.ExportedContent);
        Assert.Contains("Characters", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task SaveResults_reports_cancellation_without_error()
    {
        _service.NextResult = ToolResult<TextCounterReport>.Ok(new TextCounterReport(
            11, 10, 2, 1, 1, 1,
            System.TimeSpan.Zero, System.TimeSpan.Zero,
            System.Array.Empty<WordFrequency>()));

        var vm = CreateViewModel();
        vm.InputText = "hello world";
        _fileExport.ExportResult = false;

        await vm.SaveResultsCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, vm.StatusType);
        Assert.Equal("L.TextCounter.Status.SaveCancelled", vm.StatusText);
    }

    [Fact]
    public void Clear_resets_state()
    {
        var vm = CreateViewModel();
        vm.InputText = "abc";

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.InputText);
        Assert.Equal(0, vm.CharacterCount);
        Assert.Equal("L.TextCounter.Status.Cleared", vm.StatusText);
    }
}

// =====================================================================
// Fake Text Counter Service
// =====================================================================

internal sealed class FakeTextCounterService : ITextCounterService
{
    public ToolResult<TextCounterReport> NextResult { get; set; } = ToolResult<TextCounterReport>.Ok(
        new TextCounterReport(0, 0, 0, 0, 0, 0,
            System.TimeSpan.Zero, System.TimeSpan.Zero,
            System.Array.Empty<WordFrequency>()));

    public int MaxTextLength => 10 * 1024 * 1024;

    public ToolResult<TextCounterReport> Count(string? text)
        => NextResult;
}