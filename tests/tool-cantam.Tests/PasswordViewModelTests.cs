using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using System.Windows.Media;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Password Generator aracının sunum katmanı sözleşmesi: üretim, analiz (live),
/// hata eşlemesi, kopyalama ve temizleme davranışı.
/// </summary>
public class PasswordViewModelTests
{
    private readonly FakePasswordGeneratorService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private PasswordViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<PasswordViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.Password.Status.Ready", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Equal(16, vm.PasswordLength);
        Assert.True(vm.UseUpper);
        Assert.True(vm.UseLower);
        Assert.True(vm.UseDigits);
        Assert.True(vm.UseSpecial);
        Assert.False(vm.ExcludeSimilar);
    }

    [Fact]
    public async Task Generate_populates_password_and_strength()
    {
        _service.NextGenerate = ToolResult<PasswordReport>.Ok(new PasswordReport(
            "Ab3!XyZ9QwErTyUi",
            new PasswordStrengthReport(85, "Very Strong", "#16A34A", "Centuries", 96,
                true, true, true, true, true),
            System.TimeSpan.FromMilliseconds(3)));

        var vm = CreateViewModel();
        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.Equal("Ab3!XyZ9QwErTyUi", vm.GeneratedPassword);
        Assert.Equal("Very Strong", vm.GeneratedStrength);
        Assert.Equal(85, vm.GeneratedScore);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task Generate_error_maps_to_localized_message()
    {
        _service.NextGenerate = ToolResult<PasswordReport>.Fail(
            ToolErrorKind.Validation,
            PasswordGeneratorErrorCodes.NoCharacterSet,
            "no set");

        var vm = CreateViewModel();
        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(PasswordGeneratorErrorCodes.NoCharacterSet, vm.StatusText);
    }

    [Fact]
    public void Test_password_change_updates_strength_live()
    {
        _service.NextAnalyze = ToolResult<PasswordStrengthReport>.Ok(new PasswordStrengthReport(
            65, "Strong", "#22C55E", "3 years", 60, true, true, true, false, true));

        var vm = CreateViewModel();
        vm.TestPassword = "abcABC123";

        Assert.Equal(65, vm.StrengthPercent);
        Assert.Equal("Strong", vm.StrengthText);
        Assert.Equal("3 years", vm.CrackTime);
        Assert.Equal(60, vm.EntropyBits);
    }

    [Fact]
    public void Clearing_test_password_resets_strength()
    {
        _service.NextAnalyze = ToolResult<PasswordStrengthReport>.Ok(new PasswordStrengthReport(
            65, "Strong", "#22C55E", "3 years", 60, true, true, true, false, true));

        var vm = CreateViewModel();
        vm.TestPassword = "abcABC123";
        vm.TestPassword = string.Empty;

        Assert.Equal(0, vm.StrengthPercent);
        Assert.Equal("L.Password.Strength.StartTyping", vm.StrengthText);
        Assert.Equal(0, vm.EntropyBits);
    }

    [Fact]
    public async Task CopyPassword_writes_to_clipboard()
    {
        _service.NextGenerate = ToolResult<PasswordReport>.Ok(new PasswordReport(
            "Ab3!XyZ9", new PasswordStrengthReport(80, "Strong", "#22C55E", "years", 80, true, true, true, true, true),
            System.TimeSpan.Zero));

        var vm = CreateViewModel();
        await vm.GenerateCommand.ExecuteAsync(null);

        vm.CopyPasswordCommand.Execute(null);

        Assert.Equal("Ab3!XyZ9", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyPassword_without_result_warns()
    {
        var vm = CreateViewModel();
        vm.CopyPasswordCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Password.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public void Clear_resets_generated_and_test_password()
    {
        var vm = CreateViewModel();
        vm.GeneratedPassword = "abc";
        vm.TestPassword = "test";

        bool clearRequested = false;
        vm.OnClearRequested += () => clearRequested = true;

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.GeneratedPassword);
        Assert.Equal(string.Empty, vm.TestPassword);
        Assert.Equal("L.Password.Status.Cleared", vm.StatusText);
        Assert.True(clearRequested);
    }
}

// =====================================================================
// Fake Password Generator Service
// =====================================================================

internal sealed class FakePasswordGeneratorService : IPasswordGeneratorService
{
    public ToolResult<PasswordReport> NextGenerate { get; set; } = ToolResult<PasswordReport>.Ok(
        new PasswordReport("", new PasswordStrengthReport(0, "", "", "", 0,
            false, false, false, false, false), System.TimeSpan.Zero));

    public ToolResult<PasswordStrengthReport> NextAnalyze { get; set; } = ToolResult<PasswordStrengthReport>.Ok(
        new PasswordStrengthReport(0, "", "", "", 0, false, false, false, false, false));

    public int MinLength => 4;
    public int MaxLength => 128;

    public ToolResult<PasswordReport> Generate(PasswordOptions options) => NextGenerate;
    public ToolResult<PasswordStrengthReport> Analyze(string? password) => NextAnalyze;
}