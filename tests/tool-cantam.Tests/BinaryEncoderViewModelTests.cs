using tool_cantam.Services.ErrorCodes;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// İkili aracın sunum katmanı sözleşmesi: biçim ve önek seçimi, doğrulama, hata eşlemesi,
/// pano/kaydetme ve takas davranışı.
/// </summary>
public class BinaryEncoderViewModelTests
{
    private readonly BinaryService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private BinaryEncoderViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<BinaryEncoderViewModel>.Instance);

    [Fact]
    public void Formats_are_exposed_with_localized_names()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(2, viewModel.Formats.Count);
        Assert.Equal(BinaryFormat.Compact, viewModel.SelectedFormat.Format);
        Assert.Equal("L.Binary.Format.Compact", viewModel.SelectedFormat.DisplayName);
        Assert.False(viewModel.IncludeBytePrefix);
        Assert.Equal("L.Binary.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Encode_writes_output_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "A";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("01000001", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Binary.Status.Encoded", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Format_and_prefix_options_change_the_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "A";
        viewModel.SelectedFormat = viewModel.Formats.Single(option => option.Format == BinaryFormat.Spaced);
        viewModel.IncludeBytePrefix = true;

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("0b01000001", viewModel.OutputText);
    }

    [Fact]
    public async Task Decode_reads_bits_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "01001000 01101001";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal("Hi", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Binary.Status.Decoded", viewModel.StatusText);
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
        Assert.Equal("L.Binary.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Incomplete_last_byte_is_reported_as_an_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "0100000";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(BinaryErrorCodes.OddBitCount, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_character_is_reported_as_an_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "0100002";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(BinaryErrorCodes.InvalidDigit, viewModel.StatusText);
    }

    [Fact]
    public async Task Invalid_utf8_is_reported_as_an_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "11111111";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(BinaryErrorCodes.NotUtf8, viewModel.StatusText);
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
        Assert.Equal("L.Binary.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "A";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("01000001", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Binary.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "A";
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
        viewModel.InputText = "A";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("01000001", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "A";
        await viewModel.EncodeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Binary.Status.SaveCancelled", viewModel.StatusText);
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
        Assert.Equal("L.Binary.Status.Cleared", viewModel.StatusText);
    }
}
