using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Bir aday algoritmanın arayüzde gösterilecek hâli.</summary>
public sealed record HashCandidateRow(string Algorithm, string ConfidenceText, HashIdentifierConfidence Confidence);

/// <summary>
/// Hash Identifier aracının sunum katmanı. İş mantığı <see cref="IHashIdentifierService"/> içindedir.
/// <para>
/// Araç "bu hash şudur" demez; biçimden çıkan <b>adayları</b> ve güven düzeyini gösterir.
/// Aynı uzunluktaki algoritmalar ayırt edilemediğinde bu açıkça belirtilir.
/// </para>
/// </summary>
public partial class HashIdentifierViewModel : ObservableObject
{
    private readonly IHashIdentifierService _identifierService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<HashIdentifierViewModel> _logger;

    [ObservableProperty] private string _hashInput = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _isAmbiguous;
    [ObservableProperty] private bool _isIdentified;
    [ObservableProperty] private bool _hasResults;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public HashIdentifierViewModel(
        IHashIdentifierService identifierService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<HashIdentifierViewModel> logger)
    {
        _identifierService = identifierService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.HashIdentifier.Status.Ready");
    }

    /// <summary>Tanımlama sonucundaki adaylar.</summary>
    public ObservableCollection<HashCandidateRow> Candidates { get; } = new();

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task IdentifyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HashInput))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.HashIdentifier.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = HashInput;
            ToolResult<HashIdentificationResult> result = await Task.Run(
                () => _identifierService.Identify(input), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "identify");
                return;
            }

            HashIdentificationResult identification = result.Value!;

            Candidates.Clear();

            foreach (HashCandidate candidate in identification.Candidates)
            {
                Candidates.Add(new HashCandidateRow(
                    candidate.Algorithm,
                    _localizer.Get(ConfidenceKey(candidate.Confidence)),
                    candidate.Confidence));
            }

            HasResults = Candidates.Count > 0;
            IsIdentified = identification.IsIdentified;
            IsAmbiguous = identification.IsAmbiguous;

            string characterSetName = _localizer.Get(CharacterSetKey(identification.CharacterSet));
            SummaryText = _localizer.Format(
                "L.HashIdentifier.Summary",
                identification.Length,
                characterSetName,
                identification.Candidates.Count);

            _logger.LogInformation(
                "Hash identified: length={Length} characterSet={CharacterSet} prefix={Prefix} candidates={Count} ambiguous={Ambiguous}",
                identification.Length, identification.CharacterSet, identification.HasDistinctivePrefix,
                identification.Candidates.Count, identification.IsAmbiguous);

            if (!identification.IsIdentified)
            {
                SetStatus(NotificationType.Warning, "L.HashIdentifier.Status.NotIdentified");
                return;
            }

            SetStatus(NotificationType.Success, "L.HashIdentifier.Status.Identified", identification.Candidates.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hash identification cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash identification failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Candidates.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.HashIdentifier.Validation.NothingToCopy");
            return;
        }

        try
        {
            string payload = string.Join(Environment.NewLine,
                Candidates.Select(candidate => $"{candidate.Algorithm} ({candidate.ConfidenceText})"));

            _fileExportService.CopyToClipboard(payload);
            SetStatus(NotificationType.Success, "L.HashIdentifier.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash identification result could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelRunningOperations();

        HashInput = string.Empty;
        Candidates.Clear();
        HasResults = false;
        IsIdentified = false;
        IsAmbiguous = false;
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.HashIdentifier.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(IdentifyCancelCommand);
    }

    private static void CancelIfRunning(System.Windows.Input.ICommand? command)
    {
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
    }

    private static string ConfidenceKey(HashIdentifierConfidence confidence) => confidence switch
    {
        HashIdentifierConfidence.Certain => "L.HashIdentifier.Confidence.Certain",
        HashIdentifierConfidence.Likely => "L.HashIdentifier.Confidence.Likely",
        _ => "L.HashIdentifier.Confidence.Possible"
    };

    private static string CharacterSetKey(HashCharacterSet characterSet) => characterSet switch
    {
        HashCharacterSet.Hex => "L.HashIdentifier.Charset.Hex",
        HashCharacterSet.Base64 => "L.HashIdentifier.Charset.Base64",
        HashCharacterSet.DesCrypt => "L.HashIdentifier.Charset.DesCrypt",
        HashCharacterSet.Alphanumeric => "L.HashIdentifier.Charset.Alphanumeric",
        _ => "L.HashIdentifier.Charset.Other"
    };

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("Hash identifier {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Hash identifier {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
