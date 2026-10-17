using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Case Converter aracının sunum katmanı sözleşmesi: mod çağrıları, doğrulama,
/// hata eşlemesi, kopyalama/kaydetme ve temizleme davranışı.
/// </summary>
public class CaseConverterViewModelTests
{
    private readonly FakeCaseConverterService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private CaseConverterViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<CaseConverterViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.Case.Status.Ready", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(vm.OutputText);
    }

    [Fact]
    public async Task Empty_input_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestInputFocus += () => focusRequested = true;

        await vm.ToUpperCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Case.Validation.EmptyInput", vm.StatusText);
        Assert.False(_service.ConvertCalled);
    }

    [Theory]
    [InlineData(CaseConversionMode.Upper)]
    [InlineData(CaseConversionMode.Lower)]
    [InlineData(CaseConversionMode.Title)]
    [InlineData(CaseConversionMode.Sentence)]
    [InlineData(CaseConversionMode.Camel)]
    [InlineData(CaseConversionMode.Pascal)]
    [InlineData(CaseConversionMode.Snake)]
    [InlineData(CaseConversionMode.Kebab)]
    [InlineData(CaseConversionMode.Alternating)]
    [InlineData(CaseConversionMode.Invert)]
    public async Task Each_command_calls_the_expected_mode(CaseConversionMode mode)
    {
        _service.NextResult = ToolResult<string>.Ok("RESULT");
        var vm = CreateViewModel();
        vm.InputText = "hello";

        Task task = mode switch
        {
            CaseConversionMode.Upper => vm.ToUpperCommand.ExecuteAsync(null),
            CaseConversionMode.Lower => vm.ToLowerCommand.ExecuteAsync(null),
            CaseConversionMode.Title => vm.ToTitleCommand.ExecuteAsync(null),
            CaseConversionMode.Sentence => vm.ToSentenceCommand.ExecuteAsync(null),
            CaseConversionMode.Camel => vm.ToCamelCommand.ExecuteAsync(null),
            CaseConversionMode.Pascal => vm.ToPascalCommand.ExecuteAsync(null),
            CaseConversionMode.Snake => vm.ToSnakeCommand.ExecuteAsync(null),
            CaseConversionMode.Kebab => vm.ToKebabCommand.ExecuteAsync(null),
            CaseConversionMode.Alternating => vm.ToAlternatingCommand.ExecuteAsync(null),
            CaseConversionMode.Invert => vm.ToInvertCommand.ExecuteAsync(null),
            _ => throw new System.InvalidOperationException()
        };

        await task;

        Assert.Equal(mode, _service.LastMode);
        Assert.Equal("RESULT", vm.OutputText);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Service_error_maps_to_localized_message()
    {
        _service.NextResult = ToolResult<string>.Fail(
            ToolErrorKind.Validation,
            CaseConverterErrorCodes.InvalidUnicode,
            "bad surrogate");

        var vm = CreateViewModel();
        vm.InputText = "abc";

        await vm.ToUpperCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(CaseConverterErrorCodes.InvalidUnicode, vm.StatusText);
        Assert.Empty(vm.OutputText);
    }

    [Fact]
    public async Task Cancellation_maps_to_warning()
    {
        _service.NextResult = ToolResult<string>.Fail(
            ToolErrorKind.Cancelled,
            "L.Common.Error.Cancelled",
            "cancelled");

        var vm = CreateViewModel();
        vm.InputText = "abc";

        await vm.ToUpperCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Common.Status.Cancelled", vm.StatusText);
    }

    [Fact]
    public void Swap_exchanges_input_and_output()
    {
        var vm = CreateViewModel();
        vm.InputText = "one";
        vm.OutputText = "two";

        vm.SwapCommand.Execute(null);

        Assert.Equal("two", vm.InputText);
        Assert.Equal("one", vm.OutputText);
        Assert.Equal("L.Case.Status.Swapped", vm.StatusText);
    }

    [Fact]
    public async Task CopyOutput_writes_to_clipboard()
    {
        _service.NextResult = ToolResult<string>.Ok("HELLO");
        var vm = CreateViewModel();
        vm.InputText = "hello";
        await vm.ToUpperCommand.ExecuteAsync(null);

        vm.CopyOutputCommand.Execute(null);

        Assert.Equal("HELLO", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns()
    {
        var vm = CreateViewModel();
        vm.CopyOutputCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Case.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        _service.NextResult = ToolResult<string>.Ok("HELLO");
        var vm = CreateViewModel();
        vm.InputText = "hello";
        await vm.ToUpperCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        vm.CopyOutputCommand.Execute(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", vm.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_through_export_service()
    {
        _service.NextResult = ToolResult<string>.Ok("HELLO");
        var vm = CreateViewModel();
        vm.InputText = "hello";
        await vm.ToUpperCommand.ExecuteAsync(null);

        await vm.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("HELLO", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        _service.NextResult = ToolResult<string>.Ok("HELLO");
        var vm = CreateViewModel();
        vm.InputText = "hello";
        await vm.ToUpperCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await vm.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, vm.StatusType);
        Assert.Equal("L.Case.Status.SaveCancelled", vm.StatusText);
    }

    [Fact]
    public void Clear_resets_state()
    {
        var vm = CreateViewModel();
        vm.InputText = "abc";
        vm.OutputText = "def";

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.InputText);
        Assert.Equal(string.Empty, vm.OutputText);
        Assert.Equal("L.Case.Status.Cleared", vm.StatusText);
    }
}


internal sealed class FakeCaseConverterService : ICaseConverterService
{
    public ToolResult<string> NextResult { get; set; } = ToolResult<string>.Ok(string.Empty);
    public CaseConversionMode LastMode { get; private set; }
    public bool ConvertCalled { get; private set; }

    public int MaxTextLength => 10 * 1024 * 1024;

    public IReadOnlyList<CaseConversionMode> SupportedModes { get; } = new[]
    {
        CaseConversionMode.Upper, CaseConversionMode.Lower, CaseConversionMode.Title,
        CaseConversionMode.Sentence, CaseConversionMode.Camel, CaseConversionMode.Pascal,
        CaseConversionMode.Snake, CaseConversionMode.Kebab, CaseConversionMode.Alternating,
        CaseConversionMode.Invert
    };

    public ToolResult<string> Convert(string? text, CaseConversionMode mode)
    {
        ConvertCalled = true;
        LastMode = mode;
        return NextResult;
    }
}