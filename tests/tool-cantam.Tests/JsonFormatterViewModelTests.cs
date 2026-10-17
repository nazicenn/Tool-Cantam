using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// JSON Formatter aracının sunum katmanı sözleşmesi: format/minify/validate çağrıları,
/// doğrulama, hata eşlemesi, kopyalama/kaydetme ve temizleme davranışı.
/// </summary>
public class JsonFormatterViewModelTests
{
    private readonly FakeJsonFormatterService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private JsonFormatterViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<JsonFormatterViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.JsonFormatter.Status.Ready", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Equal(2, vm.IndentSize);
        Assert.Equal(0, vm.NodeCount);
        Assert.Equal(0, vm.JsonDepth);
    }

    [Fact]
    public async Task Empty_input_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestInputFocus += () => focusRequested = true;

        await vm.FormatCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.JsonFormatter.Validation.EmptyInput", vm.StatusText);
    }

    [Fact]
    public async Task Format_populates_output_and_stats()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
            "{\n  \"a\": 1\n}",
            NodeCount: 2,
            MaxDepth: 1,
            InputBytes: 7,
            OutputBytes: 14,
            Elapsed: System.TimeSpan.FromMilliseconds(5)));

        var vm = CreateViewModel();
        vm.InputText = "{\"a\":1}";

        await vm.FormatCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.NodeCount);
        Assert.Equal(1, vm.JsonDepth);
        Assert.Contains("7", vm.SizeText);
        Assert.Equal("{\n  \"a\": 1\n}", vm.OutputText);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Minify_calls_minify_operation()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
            "{\"a\":1}", 2, 1, 20, 7, System.TimeSpan.FromMilliseconds(2)));

        var vm = CreateViewModel();
        vm.InputText = "{ \"a\": 1 }";

        await vm.MinifyCommand.ExecuteAsync(null);

        Assert.Equal("minify", _service.LastOperation);
        Assert.Equal("{\"a\":1}", vm.OutputText);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Validate_calls_validate_operation()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
            string.Empty, 2, 1, 10, 0, System.TimeSpan.FromMilliseconds(1)));

        var vm = CreateViewModel();
        vm.InputText = "{\"a\":1}";

        await vm.ValidateCommand.ExecuteAsync(null);

        Assert.Equal("validate", _service.LastOperation);
        Assert.Empty(vm.OutputText);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Invalid_json_maps_to_localized_error()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Fail(
            ToolErrorKind.InvalidFormat,
            JsonFormatterErrorCodes.InvalidJson,
            "Line 1, position 5: unexpected token");

        var vm = CreateViewModel();
        vm.InputText = "{invalid}";

        await vm.FormatCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(JsonFormatterErrorCodes.InvalidJson, vm.StatusText);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Fail(
            ToolErrorKind.Cancelled,
            "L.Common.Error.Cancelled",
            "cancelled");

        var vm = CreateViewModel();
        vm.InputText = "{\"a\":1}";

        await vm.FormatCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public async Task CopyOutput_writes_to_clipboard()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
            "{\n  \"a\": 1\n}", 2, 1, 7, 14, System.TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.InputText = "{\"a\":1}";
        await vm.FormatCommand.ExecuteAsync(null);

        vm.CopyOutputCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("\"a\"", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns()
    {
        var vm = CreateViewModel();
        vm.CopyOutputCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.JsonFormatter.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_through_export_service()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
            "{\n  \"a\": 1\n}", 2, 1, 7, 14, System.TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.InputText = "{\"a\":1}";
        await vm.FormatCommand.ExecuteAsync(null);

        await vm.SaveOutputCommand.ExecuteAsync(null);

        Assert.NotNull(_fileExport.ExportedContent);
        Assert.Contains("\"a\"", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        _service.NextResult = ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
            "{}", 1, 1, 2, 2, System.TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.InputText = "{}";
        await vm.FormatCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await vm.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, vm.StatusType);
        Assert.Equal("L.JsonFormatter.Status.SaveCancelled", vm.StatusText);
    }

    [Fact]
    public void Clear_resets_state()
    {
        var vm = CreateViewModel();
        vm.InputText = "{\"a\":1}";
        vm.OutputText = "{}";

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.InputText);
        Assert.Equal(string.Empty, vm.OutputText);
        Assert.Equal(0, vm.NodeCount);
        Assert.Equal(0, vm.JsonDepth);
        Assert.Equal("-", vm.SizeText);
        Assert.Equal("L.JsonFormatter.Status.Cleared", vm.StatusText);
    }
}


internal sealed class FakeJsonFormatterService : IJsonFormatterService
{
    public ToolResult<JsonFormatReport> NextResult { get; set; } = ToolResult<JsonFormatReport>.Ok(
        new JsonFormatReport(string.Empty, 0, 0, 0, 0, System.TimeSpan.Zero));

    public string LastOperation { get; private set; } = string.Empty;

    public int MaxTextLength => 10 * 1024 * 1024;
    public int MaxDepth => 256;

    public ToolResult<JsonFormatReport> Format(string? json, int indentSize)
    {
        LastOperation = "format";
        return NextResult;
    }

    public ToolResult<JsonFormatReport> Minify(string? json)
    {
        LastOperation = "minify";
        return NextResult;
    }

    public ToolResult<JsonFormatReport> Validate(string? json)
    {
        LastOperation = "validate";
        return NextResult;
    }
}