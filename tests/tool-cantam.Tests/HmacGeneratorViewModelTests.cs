using tool_cantam.Services.ErrorCodes;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// HMAC aracının sunum katmanı sözleşmesi: algoritma/biçim seçimi, anahtar doğrulaması,
/// sabit zamanlı MAC doğrulaması ve pano/kaydetme davranışı.
/// </summary>
public class HmacGeneratorViewModelTests
{
    private readonly HmacService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private HmacGeneratorViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<HmacGeneratorViewModel>.Instance);

    [Fact]
    public void Default_state_uses_hmac_sha256_and_hides_the_key()
    {
        var viewModel = CreateViewModel();

        Assert.NotNull(viewModel.SelectedAlgorithm);
        Assert.Equal(HashAlgorithmKind.Sha256, viewModel.SelectedAlgorithm!.Kind);
        Assert.False(viewModel.IsSelectedAlgorithmWeak);
        Assert.False(viewModel.ShowKey);
        Assert.Equal(HmacKeyFormat.Text, viewModel.SelectedKeyFormat.Format);
        Assert.Equal(HashOutputFormat.HexUppercase, viewModel.SelectedOutputFormat.Format);
        Assert.Equal(HashMatchState.NotChecked, viewModel.VerificationState);
        Assert.Equal("L.Hmac.Status.Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task Generate_produces_the_rfc_vector_in_uppercase_hex()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "what do ya want for nothing?";
        viewModel.SecretKey = "Jefe";

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal("5BDCC146BF60754E6A042426089575C75A003F089D2739839DEC58B964EC3843", viewModel.OutputHash);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Hmac.Status.Computed", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Theory]
    [InlineData(HashOutputFormat.HexLowercase)]
    [InlineData(HashOutputFormat.Base64)]
    public async Task Output_format_changes_the_displayed_mac(HashOutputFormat format)
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "what do ya want for nothing?";
        viewModel.SecretKey = "Jefe";
        viewModel.SelectedOutputFormat = viewModel.OutputFormats.Single(option => option.Format == format);

        await viewModel.GenerateCommand.ExecuteAsync(null);

        byte[] expectedBytes = new HMACSHA256Probe().Compute("what do ya want for nothing?", "Jefe");

        string expected = format == HashOutputFormat.Base64
            ? Convert.ToBase64String(expectedBytes)
            : Convert.ToHexStringLower(expectedBytes);

        Assert.Equal(expected, viewModel.OutputHash);
    }

    [Fact]
    public async Task Hex_key_format_is_used_for_the_key()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "Hi There";
        viewModel.SecretKey = string.Concat(Enumerable.Repeat("0b", 20));
        viewModel.SelectedKeyFormat = viewModel.KeyFormats.Single(option => option.Format == HmacKeyFormat.Hex);

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal("B0344C61D8DB38535CA8AFCEAF0BF12B881DC200C9833DA726E9376C2E32CFF7", viewModel.OutputHash);
    }

    [Fact]
    public async Task Invalid_hex_key_is_reported_with_the_service_error_code()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "mesaj";
        viewModel.SecretKey = "zz";
        viewModel.SelectedKeyFormat = viewModel.KeyFormats.Single(option => option.Format == HmacKeyFormat.Hex);

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal(HmacErrorCodes.InvalidHexKey, viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputHash);
    }

    [Fact]
    public async Task Missing_key_reports_validation_and_requests_key_focus()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "mesaj";
        bool focusRequested = false;
        viewModel.RequestKeyFocus += () => focusRequested = true;

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Hmac.Validation.EmptyKey", viewModel.StatusText);
        Assert.Equal(string.Empty, viewModel.OutputHash);
    }

    [Fact]
    public async Task Matching_expected_mac_is_reported_as_a_match()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "what do ya want for nothing?";
        viewModel.SecretKey = "Jefe";

        // Kullanıcı MAC'i küçük harfli ve iki noktalı yapıştırabilir.
        viewModel.ExpectedMac = "5b:dc:c1:46:bf:60:75:4e:6a:04:24:26:08:95:75:c7:5a:00:3f:08:9d:27:39:83:9d:ec:58:b9:64:ec:38:43";

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(HashMatchState.Matched, viewModel.VerificationState);
        Assert.Equal("L.Hmac.Verify.Matched", viewModel.VerificationText);
    }

    [Fact]
    public async Task Different_expected_mac_is_reported_as_a_mismatch()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "what do ya want for nothing?";
        viewModel.SecretKey = "Jefe";
        viewModel.ExpectedMac = new string('A', 64);

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(HashMatchState.Mismatched, viewModel.VerificationState);
        Assert.Equal("L.Hmac.Verify.Mismatched", viewModel.VerificationText);
    }

    [Fact]
    public async Task Unparseable_expected_mac_is_reported_as_not_comparable()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "mesaj";
        viewModel.SecretKey = "key";
        viewModel.ExpectedMac = "bu bir mac degil!";

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(HashMatchState.NotComparable, viewModel.VerificationState);
        Assert.Equal("L.Hmac.Verify.Invalid", viewModel.VerificationText);
    }

    [Fact]
    public async Task Base64_expected_mac_is_accepted()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "what do ya want for nothing?";
        viewModel.SecretKey = "Jefe";
        viewModel.ExpectedMac = Convert.ToBase64String(new HMACSHA256Probe().Compute("what do ya want for nothing?", "Jefe"));

        await viewModel.GenerateCommand.ExecuteAsync(null);

        Assert.Equal(HashMatchState.Matched, viewModel.VerificationState);
    }

    [Fact]
    public void Selecting_a_weak_algorithm_is_flagged_for_the_view()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedAlgorithm = viewModel.Algorithms.Single(descriptor => descriptor.Kind == HashAlgorithmKind.Md5);
        Assert.True(viewModel.IsSelectedAlgorithmWeak);

        viewModel.SelectedAlgorithm = viewModel.Algorithms.Single(descriptor => descriptor.Kind == HashAlgorithmKind.Sha512);
        Assert.False(viewModel.IsSelectedAlgorithmWeak);
    }

    [Fact]
    public async Task CopyHash_puts_the_mac_on_the_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "what do ya want for nothing?";
        viewModel.SecretKey = "Jefe";
        await viewModel.GenerateCommand.ExecuteAsync(null);

        viewModel.CopyHashCommand.Execute(null);

        Assert.Equal(viewModel.OutputHash, _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyHash_without_result_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyHashCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Hmac.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyHash_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "mesaj";
        viewModel.SecretKey = "key";
        await viewModel.GenerateCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyHashCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveHash_writes_the_mac()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "mesaj";
        viewModel.SecretKey = "key";
        await viewModel.GenerateCommand.ExecuteAsync(null);

        await viewModel.SaveHashCommand.ExecuteAsync(null);

        Assert.Equal(viewModel.OutputHash, _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task Clear_wipes_every_field_including_the_key()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "mesaj";
        viewModel.SecretKey = "key";
        viewModel.ExpectedMac = "ABCD";
        viewModel.ShowKey = true;
        await viewModel.GenerateCommand.ExecuteAsync(null);

        viewModel.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.Equal(string.Empty, viewModel.SecretKey);
        Assert.Equal(string.Empty, viewModel.OutputHash);
        Assert.Equal(string.Empty, viewModel.ExpectedMac);
        Assert.False(viewModel.ShowKey);
        Assert.Equal(HashMatchState.NotChecked, viewModel.VerificationState);
        Assert.Equal("L.Hmac.Status.Cleared", viewModel.StatusText);
    }

    /// <summary>Karşılaştırma için doğrudan BCL kullanan küçük yardımcı.</summary>
    private sealed class HMACSHA256Probe
    {
        public byte[] Compute(string message, string key) =>
            new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(key))
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(message));
    }
}
