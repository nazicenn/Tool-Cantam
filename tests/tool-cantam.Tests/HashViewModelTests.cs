using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Hash aracının sunum katmanı sözleşmesi: algoritma/biçim seçimi, tüm algoritmalar modu,
/// özet karşılaştırma (normalizasyon dahil), zayıf algoritma işaretlemesi ve hata yolları.
/// </summary>
public class HashViewModelTests
{
    private readonly HashService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private HashViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<HashViewModel>.Instance);

    [Fact]
    public void Default_state_prefers_sha256_and_reports_readiness()
    {
        var viewModel = CreateViewModel();

        Assert.NotNull(viewModel.SelectedAlgorithm);
        Assert.Equal(HashAlgorithmKind.Sha256, viewModel.SelectedAlgorithm!.Kind);
        Assert.False(viewModel.IsSelectedAlgorithmWeak);
        Assert.Equal("L.Hash.Status.Ready", viewModel.StatusText);
        Assert.Empty(viewModel.Results);
        Assert.False(viewModel.HasResults);
    }

    [Fact]
    public async Task ComputeText_produces_a_single_row_with_the_expected_digest()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";

        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        HashResultRow row = Assert.Single(viewModel.Results);
        Assert.Equal("SHA-256", row.AlgorithmName);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", row.Digest);
        Assert.Equal(HashMatchState.NotChecked, row.Match);
        Assert.True(viewModel.HasResults);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.StartsWith("L.Hash.Status.Computed", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task All_algorithms_mode_produces_one_row_per_available_algorithm()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.AllAlgorithms = true;

        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        Assert.Equal(_service.Algorithms.Count, viewModel.Results.Count);
    }

    [Theory]
    [InlineData(HashOutputFormat.HexLowercase)]
    [InlineData(HashOutputFormat.Base64)]
    public async Task Output_format_changes_the_displayed_digest(HashOutputFormat format)
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.SelectedFormat = viewModel.Formats.Single(option => option.Format == format);

        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        HashResultRow row = Assert.Single(viewModel.Results);
        byte[] expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes("abc"));

        string expected = format == HashOutputFormat.Base64
            ? Convert.ToBase64String(expectedBytes)
            : Convert.ToHexStringLower(expectedBytes);

        Assert.Equal(expected, row.Digest);
    }

    [Fact]
    public async Task Expected_digest_is_matched_ignoring_separators_and_case()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        // Kullanıcı özeti başka bir araçtan küçük harfli ve iki noktalı yapıştırabilir.
        string digest = viewModel.Results[0].Digest;
        viewModel.ExpectedDigest = string.Join(":", Enumerable.Range(0, digest.Length / 2).Select(i => digest.Substring(i * 2, 2))).ToLowerInvariant();

        HashResultRow row = viewModel.Results[0];
        Assert.Equal(HashMatchState.Matched, row.Match);
        Assert.Equal("L.Hash.Match.Matched", row.MatchText);
    }

    [Fact]
    public async Task Expected_digest_mismatch_is_reported()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        viewModel.ExpectedDigest = new string('A', viewModel.Results[0].HexLength);

        Assert.Equal(HashMatchState.Mismatched, viewModel.Results[0].Match);
        Assert.Equal("L.Hash.Match.Mismatched", viewModel.Results[0].MatchText);
    }

    [Fact]
    public async Task In_all_algorithms_mode_only_the_matching_length_is_compared()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.AllAlgorithms = true;
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        // MD5 uzunluğunda bir özet: yalnızca MD5 satırı karşılaştırılabilir olmalı.
        string md5Hex = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("abc")));
        viewModel.ExpectedDigest = md5Hex;

        HashResultRow md5Row = viewModel.Results.Single(row => row.AlgorithmName == "MD5");
        HashResultRow sha256Row = viewModel.Results.Single(row => row.AlgorithmName == "SHA-256");

        Assert.Equal(HashMatchState.Matched, md5Row.Match);
        Assert.Equal(HashMatchState.NotComparable, sha256Row.Match);
        Assert.Equal("L.Hash.Match.NotComparable", sha256Row.MatchText);
    }

    [Fact]
    public async Task Clearing_the_expected_digest_resets_previous_matches()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        viewModel.ExpectedDigest = "ABCD";
        Assert.Equal(HashMatchState.NotComparable, viewModel.Results[0].Match);

        viewModel.ExpectedDigest = string.Empty;

        Assert.Equal(HashMatchState.NotChecked, viewModel.Results[0].Match);
        Assert.Equal(string.Empty, viewModel.Results[0].MatchText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.Hash.Validation.EmptyInput", viewModel.StatusText);
        Assert.Empty(viewModel.Results);
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
    public async Task ComputeFile_hashes_the_selected_file()
    {
        string path = Path.Combine(Path.GetTempPath(), "tool-cantam-vm-hash-" + Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            await File.WriteAllTextAsync(path, "abc");

            var viewModel = CreateViewModel();
            viewModel.FilePath = path;

            await viewModel.ComputeFileCommand.ExecuteAsync(null);

            HashResultRow row = Assert.Single(viewModel.Results);
            Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", row.Digest);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task File_selection_cancellation_leaves_the_state_untouched()
    {
        var viewModel = CreateViewModel();
        viewModel.FilePath = string.Empty;
        _fileExport.NextOpenPath = null;

        await viewModel.ComputeFileCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Results);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("L.Hash.Status.FileNotSelected", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyResults_copies_a_single_digest_as_is()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Equal(viewModel.Results[0].Digest, _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task CopyResults_labels_every_algorithm_when_several_are_computed()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.AllAlgorithms = true;
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        viewModel.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("SHA-256: ", _fileExport.ClipboardContent);
        Assert.Contains("MD5: ", _fileExport.ClipboardContent);
    }

    [Fact]
    public void CopyResults_without_results_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.Hash.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyResults_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveResults_writes_every_row()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        viewModel.AllAlgorithms = true;
        await viewModel.ComputeTextCommand.ExecuteAsync(null);

        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.NotNull(_fileExport.ExportedContent);
        Assert.Contains("SHA-256: ", _fileExport.ExportedContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public async Task SaveResults_reports_cancellation_without_error()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);
        _fileExport.ExportResult = false;

        await viewModel.SaveResultsCommand.ExecuteAsync(null);

        Assert.Equal(NotificationType.Info, viewModel.StatusType);
        Assert.Equal("L.Hash.Status.SaveCancelled", viewModel.StatusText);
    }

    [Fact]
    public async Task Clear_resets_results_and_expected_digest()
    {
        var viewModel = CreateViewModel();
        viewModel.InputText = "abc";
        await viewModel.ComputeTextCommand.ExecuteAsync(null);
        viewModel.ExpectedDigest = "ABCD";

        viewModel.ClearCommand.Execute(null);

        Assert.Empty(viewModel.Results);
        Assert.False(viewModel.HasResults);
        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.Equal(string.Empty, viewModel.ExpectedDigest);
        Assert.Equal("L.Hash.Status.Cleared", viewModel.StatusText);
    }
}
