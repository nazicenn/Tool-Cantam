using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Unicode aracının sunum katmanı sözleşmesi: biçim seçimi, doğrulama, hata eşlemesi,
/// pano/kaydetme ve takas davranışı.
/// </summary>
public class UnicodeConverterViewModelTests
{
    private readonly UnicodeConverterService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private UnicodeConverterViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<UnicodeConverterViewModel>.Instance);

    [Fact]
    public void Formats_are_exposed_with_localized_names()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(4, viewModel.Formats.Count);
        Assert.Equal(UnicodeEscapeFormat.Utf16Escape, viewModel.SelectedFormat.Format);
        Assert.Equal("L.Unicode.Format.Utf16", viewModel.SelectedFormat.DisplayName);
        Assert.Equal("L.Unicode.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Escape_writes_escapes_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "AB";

        await viewModel.EscapeCommand.ExecuteAsync(null);

        Assert.Equal("\\u0041\\u0042", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Unicode.Status.Escaped", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Format_option_changes_the_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "😀";
        viewModel.SelectedFormat = viewModel.Formats.Single(option => option.Format == UnicodeEscapeFormat.CodePointEscape);

        await viewModel.EscapeCommand.ExecuteAsync(null);

        Assert.Equal("\\U0001F600", viewModel.OutputText);
    }

    [Fact]
    public async Task Unescape_reads_escapes_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "\\u0041\\u0042";

        await viewModel.UnescapeCommand.ExecuteAsync(null);

        Assert.Equal("AB", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Unicode.Status.Unescaped", viewModel.StatusText);
    }

    [Fact]
    public async Task Astral_character_survives_a_round_trip_through_the_view_model()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "😀";

        await viewModel.EscapeCommand.ExecuteAsync(null);
        Assert.Equal("\\uD83D\\uDE00", viewModel.OutputText);

        viewModel.InputText = viewModel.OutputText;
        await viewModel.UnescapeCommand.ExecuteAsync(null);
        Assert.Equal("😀", viewModel.OutputText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.EscapeCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Unicode.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Lone_surrogate_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "\\uD83D";

        await viewModel.UnescapeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(UnicodeErrorCodes.LoneSurrogate, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Malformed_escape_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "\\q";

        await viewModel.UnescapeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(UnicodeErrorCodes.InvalidEscape, viewModel.StatusText);
    }

    [Fact]
    public void Swap_exchanges_input_and_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "one";
        viewModel.OutputText = "two";

        viewModel.SwapCommand.Execute(null);

        Assert.Equal("two", viewModel.InputText);
        Assert.Equal("one", viewModel.OutputText);
        Assert.Equal("L.Unicode.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "AB";
        await viewModel.EscapeCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("\\u0041\\u0042", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Unicode.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "AB";
        await viewModel.EscapeCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_the_result_through_the_export_service()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "AB";
        await viewModel.EscapeCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("\\u0041\\u0042", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "AB";
        await viewModel.EscapeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Unicode.Status.SaveCancelled", viewModel.StatusText);
    }

    [Fact]
    public void Clear_resets_input_and_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.OutputText = "def";

        viewModel.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.Equal(string.Empty, viewModel.OutputText);
        Assert.Equal("L.Unicode.Status.Cleared", viewModel.StatusText);
    }
}
