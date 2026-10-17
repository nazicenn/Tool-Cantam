using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// HTML aracının sunum katmanı sözleşmesi: seçenek anahtarları, doğrulama, hata eşlemesi,
/// pano/kaydetme ve takas davranışı.
/// </summary>
public class HtmlEncoderViewModelTests
{
    private readonly HtmlEncoderService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private HtmlEncoderViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<HtmlEncoderViewModel>.Instance);

    [Fact]
    public void Default_options_are_safe()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.UseNamedEntities);
        Assert.False(viewModel.EncodeNonAscii);
        Assert.True(viewModel.StrictEntities);
        Assert.Equal("L.Html.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Encode_writes_output_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "a < b & c";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("a &lt; b &amp; c", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Html.Status.Encoded", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task Encode_escapes_the_apostrophe_which_the_old_implementation_leaked()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "<a href='x'>";

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("&lt;a href=&#39;x&#39;&gt;", viewModel.OutputText);
    }

    [Fact]
    public async Task Numeric_reference_option_changes_the_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "&";
        viewModel.UseNamedEntities = false;

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("&#38;", viewModel.OutputText);
    }

    [Fact]
    public async Task Non_ascii_option_produces_ascii_output()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "ğ";
        viewModel.EncodeNonAscii = true;

        await viewModel.EncodeCommand.ExecuteAsync(null);

        Assert.Equal("&#287;", viewModel.OutputText);
    }

    [Fact]
    public async Task Decode_reads_entities_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "&lt;b&gt;kalın&lt;/b&gt; &amp; daha";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal("<b>kalın</b> & daha", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Html.Status.Decoded", viewModel.StatusText);
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
        Assert.Equal("L.Html.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Unknown_entity_is_reported_in_strict_mode()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "&unknown;";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(HtmlErrorCodes.UnknownEntity, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Unknown_entity_is_kept_when_strict_mode_is_off()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "&unknown;";
        viewModel.StrictEntities = false;

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.Equal("&unknown;", viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_code_point_is_reported()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "&#xD800;";

        await viewModel.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(HtmlErrorCodes.InvalidCodePoint, viewModel.StatusText);
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
        Assert.Equal("L.Html.Status.Swapped", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "<b>";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("&lt;b&gt;", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Html.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "<b>";
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
        viewModel.InputText = "<b>";
        await viewModel.EncodeCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("&lt;b&gt;", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "<b>";
        await viewModel.EncodeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Html.Status.SaveCancelled", viewModel.StatusText);
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
        Assert.Equal("L.Html.Status.Cleared", viewModel.StatusText);
    }
}
