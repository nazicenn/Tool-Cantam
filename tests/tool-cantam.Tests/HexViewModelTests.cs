using tool_cantam.Services.ErrorCodes;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Hex aracının sunum katmanı sözleşmesi: biçim ve harf büyüklüğü seçimi, doğrulama,
/// hata eşlemesi, pano/kaydetme ve takas davranışı.
/// </summary>
public class HexViewModelTests
{
    private readonly HexService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private HexViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<HexViewModel>.Instance);

    [Fact]
    public void Formats_are_exposed_with_localized_names()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(3, viewModel.Formats.Count);
        Assert.Equal(HexFormat.Compact, viewModel.SelectedFormat.Format);
        Assert.Equal("L.Hex.Format.Compact", viewModel.SelectedFormat.DisplayName);
        Assert.True(viewModel.UppercaseHex);
        Assert.Equal("L.Hex.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Encode_writes_output_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("48656C6C6F", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Hex.Status.Encoded", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Selected_format_and_case_change_the_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";
        viewModel.SelectedFormat = viewModel.Formats.Single(option => option.Format == HexFormat.Spaced);
        viewModel.UppercaseHex = false;

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("48 65 6c 6c 6f", viewModel.OutputText);
    }

    [Fact]
    public async Task Decode_reads_hex_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "48:65:6C:6C:6F";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal("Hello", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Hex.Status.Decoded", viewModel.StatusText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Hex.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Odd_digit_count_is_reported_as_an_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ABC";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(HexErrorCodes.OddLength, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_digit_is_reported_as_an_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "48 6G";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(HexErrorCodes.InvalidDigit, viewModel.StatusText);
    }

    [Fact]
    public async Task Invalid_utf8_is_reported_as_an_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "FF";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(HexErrorCodes.NotUtf8, viewModel.StatusText);
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
        Assert.Equal("L.Hex.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hi";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("4869", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Hex.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hi";
        await viewModel.EncodeCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_the_result_through_the_export_service()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hi";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("4869", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hi";
        await viewModel.EncodeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Hex.Status.SaveCancelled", viewModel.StatusText);
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
        Assert.Equal("L.Hex.Status.Cleared", viewModel.StatusText);
    }
}
