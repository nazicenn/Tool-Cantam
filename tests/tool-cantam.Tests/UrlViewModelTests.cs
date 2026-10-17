using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// URL aracının sunum katmanı sözleşmesi: mod seçimi, doğrulama, hata eşlemesi,
/// pano/kaydetme ve takas davranışı.
/// </summary>
public class UrlViewModelTests
{
    private readonly UrlService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private UrlViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<UrlViewModel>.Instance);

    [Fact]
    public void Modes_are_exposed_with_localized_names()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(3, viewModel.Modes.Count);
        Assert.Equal(UrlEncodingMode.Component, viewModel.SelectedMode.Mode);
        Assert.Equal("L.Url.Mode.Component", viewModel.SelectedMode.DisplayName);
        Assert.Equal("L.Url.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Encode_writes_output_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "a b";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("a%20b", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Url.Status.Encoded", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Selected_mode_changes_the_result()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "a b";

        viewModel.SelectedMode = viewModel.Modes.Single(option => option.Mode == UrlEncodingMode.Form);
        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("a+b", viewModel.OutputText);
    }

    [Fact]
    public async Task Decode_reads_escapes_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "caf%C3%A9";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal("café", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Url.Status.Decoded", viewModel.StatusText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Url.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_escape_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc%zz";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(UrlErrorCodes.InvalidEscape, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_utf8_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "%FF";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(UrlErrorCodes.NotUtf8, viewModel.StatusText);
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
        Assert.Equal("L.Url.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "a b";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("a%20b", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Url.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "a b";
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
        viewModel.InputText = "a b";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("a%20b", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "a b";
        await viewModel.EncodeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Url.Status.SaveCancelled", viewModel.StatusText);
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
        Assert.Equal("L.Url.Status.Cleared", viewModel.StatusText);
    }
}
