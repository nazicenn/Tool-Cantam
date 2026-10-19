using tool_cantam.Services.ErrorCodes;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// ROT-N aracının sunum katmanı sözleşmesi: kaydırma seçimi, kodlama/çözme yönü,
/// doğrulama, hata eşlemesi ve pano/kaydetme davranışı.
/// </summary>
public class Rot13ViewModelTests
{
    private readonly Rot13Service _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private Rot13ViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<Rot13ViewModel>.Instance);

    [Fact]
    public void Default_state_is_classic_rot13()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(13, viewModel.Shift);
        Assert.True(viewModel.IsSelfInverse);
        Assert.Equal("L.Rot13.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Encode_rotates_letters_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("Uryyb", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Rot13.Status.Encoded", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Decode_with_shift_thirteen_gives_the_same_result_as_encode()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal("Uryyb", viewModel.OutputText);
        Assert.StartsWith("L.Rot13.Status.Decoded", viewModel.StatusText);
    }

    [Fact]
    public async Task Decode_reverses_encode_for_a_shift_other_than_thirteen()
    {
        var viewModel = CreateViewModel();
        viewModel.Shift = 5;
        viewModel.InputText = "abc";

        await viewModel.EncodeCommand.ExecuteAsync(null);
        Assert.Equal("fgh", viewModel.OutputText);

        viewModel.InputText = viewModel.OutputText;
        await viewModel.DecodeCommand.ExecuteAsync(null);
        Assert.Equal("abc", viewModel.OutputText);
    }

    [Fact]
    public void Self_inverse_flag_follows_the_shift()
    {
        var viewModel = CreateViewModel();

        viewModel.Shift = 7;
        Assert.False(viewModel.IsSelfInverse);

        viewModel.Shift = 13;
        Assert.True(viewModel.IsSelfInverse);
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
        Assert.Equal("L.Rot13.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_unicode_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "\uD800";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(Rot13ErrorCodes.InvalidUnicode, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
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
        Assert.Equal("L.Rot13.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("Uryyb", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Rot13.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";
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
        viewModel.InputText = "Hello";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("Uryyb", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hello";
        await viewModel.EncodeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Rot13.Status.SaveCancelled", viewModel.StatusText);
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
        Assert.Equal("L.Rot13.Status.Cleared", viewModel.StatusText);
    }
}
