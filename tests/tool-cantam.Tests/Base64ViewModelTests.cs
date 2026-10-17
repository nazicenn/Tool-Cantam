using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Base64 aracının sunum katmanı sözleşmesi: doğrulama, hata eşlemesi, pano ve durum bildirimi.
/// ViewModel hiçbir metni koda gömmez ve hiçbir durumda istisna sızdırmaz.
/// </summary>
public class Base64ViewModelTests
{
    private readonly Base64Service _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private Base64ViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<Base64ViewModel>.Instance);

    [Fact]
    public async Task EncodeText_writes_output_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "foobar";

        await viewModel.EncodeTextCommand.ExecuteAsync(null);

        Assert.Equal("Zm9vYmFy", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Base64.Status.Encoded", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task DecodeText_writes_output_and_reports_success()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Zm9vYmFy";

        await viewModel.DecodeTextCommand.ExecuteAsync(null);

        Assert.Equal("foobar", viewModel.OutputText);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Base64.Status.Decoded", viewModel.StatusText);
    }

    [Fact]
    public async Task UrlSafe_switch_changes_the_alphabet_used()
    {
        var viewModel = CreateViewModel();
        viewModel.UseUrlSafeAlphabet = true;
        viewModel.InputText = "\uFFFFa";

        await viewModel.EncodeTextCommand.ExecuteAsync(null);

        Assert.Equal("77-_YQ", viewModel.OutputText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.EncodeTextCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Base64.Validation.EmptyInput", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Invalid_base64_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Zm9v!";

        await viewModel.DecodeTextCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(Base64ErrorCodes.InvalidCharacter, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputText);
    }

    [Fact]
    public async Task Binary_content_is_reported_instead_of_producing_broken_text()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = Convert.ToBase64String(new byte[] { 0xFF, 0xFE });

        await viewModel.DecodeTextCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(Base64ErrorCodes.NotUtf8Text, viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_puts_the_result_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "foobar";
        await viewModel.EncodeTextCommand.ExecuteAsync(null);

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal("Zm9vYmFy", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyOutputCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Base64.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "foobar";
        await viewModel.EncodeTextCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        // İstisna dışarı sızmamalı; kullanıcı bilgilendirilmeli.
        viewModel.CopyOutputCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_the_result_through_the_export_service()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "foobar";
        await viewModel.EncodeTextCommand.ExecuteAsync(null);

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("Zm9vYmFy", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "foobar";
        await viewModel.EncodeTextCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Base64.Status.SaveCancelled", viewModel.StatusText);
    }

    [Fact]
    public void Clear_resets_input_output_and_file_selection()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.FilePath = @"C:\temp\file.bin";

        viewModel.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.Equal(string.Empty, viewModel.OutputText);
        Assert.Equal(string.Empty, viewModel.FilePath);
        Assert.Equal("L.Base64.Status.Cleared", viewModel.StatusText);
    }

    [Fact]
    public async Task EncodeFile_converts_an_existing_file_and_reports_the_result()
    {
        string directory = Path.Combine(Path.GetTempPath(), "tool-cantam-vm-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string source = Path.Combine(directory, "source.bin");
            string destination = Path.Combine(directory, "encoded.b64");
            await File.WriteAllTextAsync(source, "foobar", TestContext.Current.CancellationToken);

            var viewModel = CreateViewModel();
            viewModel.FilePath = source;
            _fileExport.NextSavePath = destination;

            await viewModel.EncodeFileCommand.ExecuteAsync(null);

            Assert.Equal(NotificationType.Success, viewModel.StatusType);
            Assert.StartsWith("L.Base64.Status.FileEncoded", viewModel.StatusText);
            Assert.True(File.Exists(destination));
            Assert.Equal("Zm9vYmFy", (await File.ReadAllTextAsync(destination, TestContext.Current.CancellationToken)).Trim());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DecodeFile_reports_invalid_content_as_an_error()
    {
        string directory = Path.Combine(Path.GetTempPath(), "tool-cantam-vm-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string source = Path.Combine(directory, "broken.b64");
            string destination = Path.Combine(directory, "out.bin");
            await File.WriteAllTextAsync(source, "not base64 ###", TestContext.Current.CancellationToken);

            var viewModel = CreateViewModel();
            viewModel.FilePath = source;
            _fileExport.NextSavePath = destination;

            await viewModel.DecodeFileCommand.ExecuteAsync(null);

            Assert.Equal(NotificationType.Error, viewModel.StatusType);
            Assert.Equal(Base64ErrorCodes.InvalidFileContent, viewModel.StatusText);
            Assert.False(File.Exists(destination));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Cancelling_a_file_selection_leaves_the_state_untouched()
    {
        var viewModel = CreateViewModel();
        viewModel.FilePath = string.Empty;
        _fileExport.NextOpenPath = null;

        await viewModel.EncodeFileCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsBusy);
        Assert.Equal(string.Empty, viewModel.FilePath);
    }
}
