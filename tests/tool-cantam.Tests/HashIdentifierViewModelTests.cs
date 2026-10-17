using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Enums;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Hash tanımlayıcının sunum katmanı sözleşmesi: aday listesi, güven etiketleri, belirsizlik
/// bildirimi, tanımlanamayan girdi ve pano temizleme davranışı.
/// </summary>
public class HashIdentifierViewModelTests
{
    private readonly HashIdentifierService _service = new();
    private readonly FakeFileExportService _fileExport = new();
    private readonly FakeLocalizerService _localizer = new();

    private HashIdentifierViewModel CreateViewModel() =>
        new(_service, _fileExport, _localizer, NullLogger<HashIdentifierViewModel>.Instance);

    [Fact]
    public async Task Thirty_two_hex_characters_produce_three_candidate_rows()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = new string('a', 32);

        await viewModel.IdentifyCommand.ExecuteAsync(null);

        Assert.Equal(3, viewModel.Candidates.Count);
        Assert.True(viewModel.HasResults);
        Assert.True(viewModel.IsIdentified);
        Assert.True(viewModel.IsAmbiguous);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
        Assert.Contains("MD5", viewModel.Candidates.Select(row => row.Algorithm));
        Assert.All(viewModel.Candidates, row => Assert.Equal("L.HashIdentifier.Confidence.Possible", row.ConfidenceText));
    }

    [Fact]
    public async Task Summary_reports_length_character_set_and_candidate_count()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = new string('a', 32);

        await viewModel.IdentifyCommand.ExecuteAsync(null);

        Assert.StartsWith("L.HashIdentifier.Summary|32|L.HashIdentifier.Charset.Hex|3", viewModel.SummaryText);
    }

    [Fact]
    public async Task Bcrypt_value_is_reported_as_certain()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = "$2b$12$abcdefghijklmnopqrstuv";

        await viewModel.IdentifyCommand.ExecuteAsync(null);

        HashCandidateRow row = Assert.Single(viewModel.Candidates);
        Assert.Equal("bcrypt", row.Algorithm);
        Assert.Equal(HashIdentifierConfidence.Certain, row.Confidence);
        Assert.Equal("L.HashIdentifier.Confidence.Certain", row.ConfidenceText);
        Assert.False(viewModel.IsAmbiguous);
    }

    [Fact]
    public async Task Unrecognised_value_reports_that_nothing_matched()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = "this is not a hash";

        await viewModel.IdentifyCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Candidates);
        Assert.False(viewModel.HasResults);
        Assert.False(viewModel.IsIdentified);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.HashIdentifier.Status.NotIdentified", viewModel.StatusText);
    }

    [Fact]
    public async Task Missing_input_reports_validation_and_requests_focus()
    {
        var viewModel = CreateViewModel();
        bool focusRequested = false;
        viewModel.RequestInputFocus += () => focusRequested = true;

        await viewModel.IdentifyCommand.ExecuteAsync(null);

        Assert.True(focusRequested);
        Assert.Equal(NotificationType.Warning, viewModel.StatusType);
        Assert.Equal("L.HashIdentifier.Validation.EmptyInput", viewModel.StatusText);
        Assert.Empty(viewModel.Candidates);
    }

    [Fact]
    public async Task CopyResults_writes_algorithm_and_confidence()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = "$2b$12$abcdefghijklmnopqrstuv";
        await viewModel.IdentifyCommand.ExecuteAsync(null);

        viewModel.CopyResultsCommand.Execute(null);

        Assert.NotNull(_fileExport.ClipboardContent);
        Assert.Contains("bcrypt", _fileExport.ClipboardContent);
        Assert.Contains("L.HashIdentifier.Confidence.Certain", _fileExport.ClipboardContent);
        Assert.Equal(NotificationType.Success, viewModel.StatusType);
    }

    [Fact]
    public void CopyResults_without_results_warns_instead_of_copying()
    {
        var viewModel = CreateViewModel();

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Null(_fileExport.ClipboardContent);
        Assert.Equal("L.HashIdentifier.Validation.NothingToCopy", viewModel.StatusText);
    }

    [Fact]
    public async Task CopyResults_survives_a_busy_clipboard()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = "$2b$12$abcdefghijklmnopqrstuv";
        await viewModel.IdentifyCommand.ExecuteAsync(null);
        _fileExport.ClipboardThrows = true;

        viewModel.CopyResultsCommand.Execute(null);

        Assert.Equal(NotificationType.Error, viewModel.StatusType);
        Assert.Equal("L.Common.Error.Clipboard", viewModel.StatusText);
    }

    [Fact]
    public async Task Clear_resets_every_displayed_value()
    {
        var viewModel = CreateViewModel();
        viewModel.HashInput = new string('a', 32);
        await viewModel.IdentifyCommand.ExecuteAsync(null);

        viewModel.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.HashInput);
        Assert.Empty(viewModel.Candidates);
        Assert.False(viewModel.HasResults);
        Assert.False(viewModel.IsIdentified);
        Assert.False(viewModel.IsAmbiguous);
        Assert.Equal(string.Empty, viewModel.SummaryText);
        Assert.Equal("L.HashIdentifier.Status.Cleared", viewModel.StatusText);
    }
}
