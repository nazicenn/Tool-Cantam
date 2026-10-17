using System;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// JWT Decoder aracının sunum katmanı sözleşmesi: decode, süre dolumu durumu,
/// hata eşlemesi, kopyalama/kaydetme ve temizleme davranışı.
/// </summary>
public class JwtViewModelTests
{
    private readonly FakeJwtService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private JwtViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<JwtViewModel>.Instance);

    [Fact]
    public void Initial_state_shows_ready()
    {
        var vm = CreateViewModel();
        Assert.Equal("L.Jwt.Status.Ready", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.False(vm.HasResult);
        Assert.Equal("-", vm.Algorithm);
    }

    [Fact]
    public async Task Empty_input_reports_validation_and_requests_focus()
    {
        var vm = CreateViewModel();
        bool focusRequested = false;
        vm.RequestInputFocus += () => focusRequested = true;

        await vm.DecodeCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, vm.StatusType);
        Assert.Equal("L.Jwt.Validation.EmptyInput", vm.StatusText);
    }

    [Fact]
    public async Task Successful_decode_populates_fields()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            Header: "{\n  \"alg\": \"HS256\"\n}",
            Payload: "{\n  \"sub\": \"1234\"\n}",
            Signature: "sig",
            Algorithm: "HS256",
            TokenType: "JWT",
            Issuer: null,
            Subject: "1234",
            Audience: null,
            IssuedAt: DateTimeOffset.UtcNow,
            NotBefore: null,
            Expiration: DateTimeOffset.UtcNow.AddHours(2),
            JwtId: null,
            IsExpired: false,
            IsNotYetValid: false,
            TimeUntilExpiry: TimeSpan.FromHours(2),
            Elapsed: TimeSpan.FromMilliseconds(5)));

        var vm = CreateViewModel();
        vm.JwtInput = "token";

        await vm.DecodeCommand.ExecuteAsync(null);

        Assert.Equal("HS256", vm.Algorithm);
        Assert.Equal("JWT", vm.TokenType);
        Assert.Contains("HS256", vm.DecodedHeader);
        Assert.Contains("1234", vm.DecodedPayload);
        Assert.True(vm.HasResult);
        Assert.Equal(NotificationType.Success, vm.StatusType);
        Assert.Equal(NotificationType.Success, vm.ExpiryStatusType);
    }

    [Fact]
    public async Task Expired_token_shows_error_status()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            Header: "{}", Payload: "{}", Signature: "sig",
            Algorithm: "HS256", TokenType: "JWT",
            Issuer: null, Subject: null, Audience: null,
            IssuedAt: null, NotBefore: null,
            Expiration: DateTimeOffset.UtcNow.AddHours(-1),
            JwtId: null,
            IsExpired: true,
            IsNotYetValid: false,
            TimeUntilExpiry: TimeSpan.FromHours(-1),
            Elapsed: TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.JwtInput = "token";

        await vm.DecodeCommand.ExecuteAsync(null);

        Assert.True(vm.HasResult);
        Assert.Equal(NotificationType.Error, vm.ExpiryStatusType);
        Assert.Equal("L.Jwt.Expiry.Expired", vm.ExpiryStatus);
    }

    [Fact]
    public async Task Token_without_expiration_shows_no_exp()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            Header: "{}", Payload: "{}", Signature: "sig",
            Algorithm: "HS256", TokenType: "JWT",
            Issuer: null, Subject: null, Audience: null,
            IssuedAt: null, NotBefore: null, Expiration: null,
            JwtId: null,
            IsExpired: false, IsNotYetValid: false,
            TimeUntilExpiry: null,
            Elapsed: TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.JwtInput = "token";

        await vm.DecodeCommand.ExecuteAsync(null);

        Assert.True(vm.HasResult);
        Assert.Equal(NotificationType.Info, vm.ExpiryStatusType);
        Assert.Equal("L.Jwt.Expiry.NoExp", vm.ExpiresAt);
    }

    [Fact]
    public async Task Invalid_format_maps_to_localized_error()
    {
        _service.NextResult = ToolResult<JwtReport>.Fail(
            ToolErrorKind.InvalidFormat,
            JwtErrorCodes.InvalidFormat,
            "must have 2 or 3 parts");

        var vm = CreateViewModel();
        vm.JwtInput = "bad";

        await vm.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(JwtErrorCodes.InvalidFormat, vm.StatusText);
        Assert.False(vm.HasResult);
    }

    [Fact]
    public async Task Invalid_base64_maps_to_localized_error()
    {
        _service.NextResult = ToolResult<JwtReport>.Fail(
            ToolErrorKind.InvalidFormat,
            JwtErrorCodes.InvalidBase64,
            "not base64");

        var vm = CreateViewModel();
        vm.JwtInput = "!!!.!!.!!";

        await vm.DecodeCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, vm.StatusType);
        Assert.Equal(JwtErrorCodes.InvalidBase64, vm.StatusText);
    }

    [Fact]
    public async Task CopyPayload_writes_to_clipboard()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            "H", "P", "S", "HS256", "JWT",
            null, null, null, null, null, null, null,
            false, false, null, TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.JwtInput = "token";
        await vm.DecodeCommand.ExecuteAsync(null);

        vm.CopyPayloadCommand.Execute(null);

        Assert.Equal("P", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task CopyHeader_writes_to_clipboard()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            "H", "P", "S", "HS256", "JWT",
            null, null, null, null, null, null, null,
            false, false, null, TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.JwtInput = "token";
        await vm.DecodeCommand.ExecuteAsync(null);

        vm.CopyHeaderCommand.Execute(null);

        Assert.Equal("H", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyPayload_without_result_warns()
    {
        var vm = CreateViewModel();
        vm.CopyPayloadCommand.Execute(null);
        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Jwt.Validation.NothingToCopy", vm.StatusText);
    }

    [Fact]
    public async Task SavePayload_writes_through_export_service()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            "H", "P", "S", "HS256", "JWT",
            null, null, null, null, null, null, null,
            false, false, null, TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.JwtInput = "token";
        await vm.DecodeCommand.ExecuteAsync(null);

        await vm.SavePayloadCommand.ExecuteAsync(null);

        Assert.Equal("P", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public async Task SavePayload_reports_cancellation_without_error()
    {
        _service.NextResult = ToolResult<JwtReport>.Ok(new JwtReport(
            "H", "P", "S", "HS256", "JWT",
            null, null, null, null, null, null, null,
            false, false, null, TimeSpan.Zero));

        var vm = CreateViewModel();
        vm.JwtInput = "token";
        await vm.DecodeCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await vm.SavePayloadCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, vm.StatusType);
        Assert.Equal("L.Jwt.Status.SaveCancelled", vm.StatusText);
    }

    [Fact]
    public void Clear_resets_state()
    {
        var vm = CreateViewModel();
        vm.JwtInput = "token";
        vm.DecodedHeader = "H";
        vm.DecodedPayload = "P";

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.JwtInput);
        Assert.Equal(string.Empty, vm.DecodedHeader);
        Assert.Equal(string.Empty, vm.DecodedPayload);
        Assert.False(vm.HasResult);
        Assert.Equal("L.Jwt.Status.Cleared", vm.StatusText);
    }
}

// =====================================================================
// Fake JWT Service
// =====================================================================

internal sealed class FakeJwtService : IJwtService
{
    public ToolResult<JwtReport> NextResult { get; set; } = ToolResult<JwtReport>.Ok(
        new JwtReport("", "", "", "", "", null, null, null, null, null, null, null,
            false, false, null, TimeSpan.Zero));

    public int MaxTokenLength => 100 * 1024;

    public ToolResult<JwtReport> Decode(string? jwt) => NextResult;
}