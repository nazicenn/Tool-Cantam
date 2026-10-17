using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// String Reverser aracının sunum katmanı sözleşmesi: 5 mod çağrısı, doğrulama,
/// hata eşlemesi, kopyalama/kaydetme ve temizleme davranışı.
/// </summary>
public class StringReverserViewModelTests
{
    private readonly FakeStringReverserService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private StringReverserViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<StringReverserViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.StringReverser.Status.Ready", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(vm.OutputText);
    }

    [Fact]
    public async Task Empty_input_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestInputFocus += () => focusRequested = true;

        await vm.ReverseAllCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.StringReverser.Validation.EmptyInput", vm.StatusText);
        Assert.False(_service.ReverseCalled);
    }

    [Theory]
    [InlineData(StringReverseMode.ReverseAll)]
    [InlineData(StringReverseMode.ReverseWords)]
    [InlineData(StringReverseMode.ReverseEachWord)]
    [InlineData(StringReverseMode.ReverseLines)]
    [InlineData(StringReverseMode.ReverseLinesAndContent)]
    public async Task Each_command_calls_the_expected_mode(StringReverseMode mode)
    {
        _service.NextResult = ToolResult<string>.Ok("RESULT");
        var vm = CreateViewModel();
        vm.InputText = "abc";

        Task task = mode switch
        {
            StringReverseMode.ReverseAll => vm.ReverseAllCommand.ExecuteAsync(null),
            StringReverseMode.ReverseWords => vm.ReverseWordsCommand.ExecuteAsync(null),
            StringReverseMode.ReverseEachWord => vm.ReverseEachWordCommand.ExecuteAsync(null),
            StringReverseMode.ReverseLines => vm.ReverseLinesCommand.ExecuteAsync(null),
            StringReverseMode.ReverseLinesAndContent => vm.ReverseLinesAndContentCommand.ExecuteAsync(null),
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
            StringReverserErrorCodes.InvalidUnicode,
            "bad surrogate");

        var vm = CreateViewModel();
        vm.InputText = "abc";

        await vm.ReverseAllCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(StringReverserErrorCodes.InvalidUnicode, vm.StatusText);
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

        await vm.ReverseAllCommand.ExecuteAsync(null);

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
        Assert.Equal("L.StringReverser.Status.Swapped", vm.StatusText);
    }

    [Fact]
    public async Task CopyOutput_writes_to_clipboard()
    {
        _service.NextResult = ToolResult<string>.Ok("cba");
        var vm = CreateViewModel();
        vm.InputText = "abc";
        await vm.ReverseAllCommand.ExecuteAsync(null);

        vm.CopyOutputCommand.Execute(null);

        Assert.Equal("cba", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyOutput_without_result_warns()
    {
        var vm = CreateViewModel();
        vm.CopyOutputCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.StringReverser.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task CopyOutput_survives_a_busy_clipboard()
    {
        _service.NextResult = ToolResult<string>.Ok("cba");
        var vm = CreateViewModel();
        vm.InputText = "abc";
        await vm.ReverseAllCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        vm.CopyOutputCommand.Execute(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", vm.StatusText);
    }

    [Fact]
    public async Task SaveOutput_writes_through_export_service()
    {
        _service.NextResult = ToolResult<string>.Ok("cba");
        var vm = CreateViewModel();
        vm.InputText = "abc";
        await vm.ReverseAllCommand.ExecuteAsync(null);

        await vm.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal("cba", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task SaveOutput_reports_cancellation_without_error()
    {
        _service.NextResult = ToolResult<string>.Ok("cba");
        var vm = CreateViewModel();
        vm.InputText = "abc";
        await vm.ReverseAllCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await vm.SaveOutputCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, vm.StatusType);
        Assert.Equal("L.StringReverser.Status.SaveCancelled", vm.StatusText);
    }

    [Fact]
    public void Clear_resets_state()
    {
        var vm = CreateViewModel();
        vm.InputText = "abc";
        vm.OutputText = "cba";

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.InputText);
        Assert.Equal(string.Empty, vm.OutputText);
        Assert.Equal("L.StringReverser.Status.Cleared", vm.StatusText);
    }
}


internal sealed class FakeStringReverserService : IStringReverserService
{
    public ToolResult<string> NextResult { get; set; } = ToolResult<string>.Ok(string.Empty);
    public StringReverseMode LastMode { get; private set; }
    public bool ReverseCalled { get; private set; }

    public int MaxTextLength => 10 * 1024 * 1024;

    public IReadOnlyList<StringReverseMode> SupportedModes { get; } = new[]
    {
        StringReverseMode.ReverseAll,
        StringReverseMode.ReverseWords,
        StringReverseMode.ReverseEachWord,
        StringReverseMode.ReverseLines,
        StringReverseMode.ReverseLinesAndContent
    };

    public ToolResult<string> Reverse(string? text, StringReverseMode mode)
    {
        ReverseCalled = true;
        LastMode = mode;
        return NextResult;
    }
}