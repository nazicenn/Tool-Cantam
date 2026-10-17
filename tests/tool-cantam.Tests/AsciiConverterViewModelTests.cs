using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Karakter kodu aracının sunum katmanı sözleşmesi: biçim seçimi, doğrulama, hata eşlemesi,
/// pano/kaydetme ve takas davranışı.
/// </summary>
public class AsciiConverterViewModelTests
{
    private readonly AsciiConverterService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private AsciiConverterViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<AsciiConverterViewModel>.Instance);

    [Fact]
    public void Formats_are_exposed_with_localized_names()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(2, viewModel.Formats.Count);
        Assert.Equal(CharacterCodeFormat.Decimal, viewModel.SelectedFormat.Format);
        Assert.Equal("L.CharacterCodes.Format.Decimal", viewModel.SelectedFormat.DisplayName);
        Assert.Equal("L.CharacterCodes.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task ToCodes_writes_codes_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ABC";

        await viewModel.ToCodesCommand.ExecuteAsync(null);

        Assert.Equal("65 66 67", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.CharacterCodes.Status.ConvertedToCodes", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Format_option_changes_the_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "A";
        viewModel.SelectedFormat = viewModel.Formats.Single(option => option.Format == CharacterCodeFormat.Hexadecimal);

        await viewModel.ToCodesCommand.ExecuteAsync(null);

        Assert.Equal("U+0041", viewModel.OutputText);
    }

    [Fact]
    public async Task ToText_reads_codes_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "65 66 67";

        await viewModel.ToTextCommand.ExecuteAsync(null);

        Assert.Equal("ABC", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.CharacterCodes.Status.ConvertedToText", viewModel.StatusText);
    }

    [Fact]
    public async Task Emoji_survives_a_round_trip_through_the_view_model()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "😀";

        await viewModel.ToCodesCommand.ExecuteAsync(null);
        Assert.Equal("128512", viewModel.OutputText);

        viewModel.InputText = viewModel.OutputText;
        await viewModel.ToTextCommand.ExecuteAsync(null);
        Assert.Equal("😀", viewModel.OutputText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.ToCodesCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.CharacterCodes.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_token_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";

        await viewModel.ToTextCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(CharacterCodeErrorCodes.InvalidToken, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_code_point_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "55296";

        await viewModel.ToTextCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(CharacterCodeErrorCodes.InvalidCodePoint, viewModel.StatusText);
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
        Assert.Equal("L.CharacterCodes.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ABC";
        await viewModel.ToCodesCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("65 66 67", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.CharacterCodes.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ABC";
        await viewModel.ToCodesCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_the_result_through_the_export_service()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ABC";
        await viewModel.ToCodesCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("65 66 67", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ABC";
        await viewModel.ToCodesCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.CharacterCodes.Status.SaveCancelled", viewModel.StatusText);
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
        Assert.Equal("L.CharacterCodes.Status.Cleared", viewModel.StatusText);
    }
}
