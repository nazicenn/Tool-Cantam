using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Özet gösterim biçiminin arayüzde gösterilecek hâli.</summary>
public sealed record HashOutputFormatOption(HashOutputFormat Format, string DisplayName);

/// <summary>
/// Hash Generator aracının sunum katmanı. İş mantığı <see cref="IHashService"/> içindedir.
/// <para>
/// Araç, seçilen algoritma veya tüm algoritmalar için özet üretir; girilen beklenen özetle
/// karşılaştırır ve zayıf algoritmaları (MD5, SHA-1) görünür biçimde işaretler.
/// </para>
/// </summary>
public partial class HashViewModel : ObservableObject
{
    private readonly IHashService _hashService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<HashViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private HashAlgorithmDescriptor? _selectedAlgorithm;
    [ObservableProperty] private bool _allAlgorithms;
    [ObservableProperty] private HashOutputFormatOption _selectedFormat;
    [ObservableProperty] private string _expectedDigest = string.Empty;
    [ObservableProperty] private bool _hasResults;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public HashViewModel(
        IHashService hashService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<HashViewModel> logger)
    {
        _hashService = hashService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        Formats = new[]
        {
            new HashOutputFormatOption(HashOutputFormat.HexUppercase, _localizer.Get("L.Hash.Format.HexUpper")),
            new HashOutputFormatOption(HashOutputFormat.HexLowercase, _localizer.Get("L.Hash.Format.HexLower")),
            new HashOutputFormatOption(HashOutputFormat.Base64, _localizer.Get("L.Hash.Format.Base64"))
        };

        _selectedFormat = Formats[0];
        _selectedAlgorithm = HashService_DefaultAlgorithm();
        StatusText = _localizer.Get("L.Hash.Status.Ready");
    }

    /// <summary>Bu makinede kullanılabilen algoritmalar.</summary>
    public IReadOnlyList<HashAlgorithmDescriptor> Algorithms => _hashService.Algorithms;

    /// <summary>Seçilebilir çıktı biçimleri.</summary>
    public IReadOnlyList<HashOutputFormatOption> Formats { get; }

    /// <summary>Hesaplanan özetler (tek algoritma veya tümü).</summary>
    public ObservableCollection<HashResultRow> Results { get; } = new();

    private HashAlgorithmDescriptor? HashService_DefaultAlgorithm()
    {
        foreach (HashAlgorithmDescriptor descriptor in _hashService.Algorithms)
        {
            if (!descriptor.IsWeak && descriptor.Kind == HashAlgorithmKind.Sha256)
                return descriptor;
        }

        foreach (HashAlgorithmDescriptor descriptor in _hashService.Algorithms)
        {
            if (!descriptor.IsWeak)
                return descriptor;
        }

        return _hashService.Algorithms.Count > 0 ? _hashService.Algorithms[0] : null;
    }

    /// <summary>Seçili algoritma zayıf mı; arayüz uyarı gösterir.</summary>
    public bool IsSelectedAlgorithmWeak => SelectedAlgorithm?.IsWeak == true;

    partial void OnSelectedAlgorithmChanged(HashAlgorithmDescriptor? value) =>
        OnPropertyChanged(nameof(IsSelectedAlgorithmWeak));

    partial void OnExpectedDigestChanged(string value) => UpdateMatches();

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ComputeTextAsync(CancellationToken cancellationToken) => ComputeAsync(fromFile: false, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ComputeFileAsync(CancellationToken cancellationToken) => ComputeAsync(fromFile: true, cancellationToken);

    private async Task ComputeAsync(bool fromFile, CancellationToken cancellationToken)
    {
        string? path = null;

        if (fromFile)
        {
            path = ResolveFile();
            if (path is null)
            {
                SetStatus(NotificationType.Info, "L.Hash.Status.FileNotSelected");
                return;
            }
        }
        else if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Hash.Validation.EmptyInput");
            return;
        }

        HashAlgorithmDescriptor[] descriptors = ResolveDescriptors();
        if (descriptors.Length == 0)
        {
            SetStatus(NotificationType.Warning, "L.Hash.Validation.NoAlgorithm");
            return;
        }

        HashOutputFormat format = SelectedFormat.Format;
        string input = InputText;

        IsBusy = true;
        try
        {
            var rows = new List<HashResultRow>(descriptors.Length);

            foreach (HashAlgorithmDescriptor descriptor in descriptors)
            {
                ToolResult<byte[]> result = fromFile
                    ? await _hashService.ComputeFromFileAsync(path!, descriptor.Kind, cancellationToken)
                    : await Task.Run(() => _hashService.ComputeFromText(input, descriptor.Kind), cancellationToken);

                if (!result.IsSuccess)
                {
                    ReportFailure(result, fromFile ? "hash-file" : "hash-text");
                    return;
                }

                rows.Add(BuildRow(descriptor, result.Value!, format));
            }

            Results.Clear();
            foreach (HashResultRow row in rows)
                Results.Add(row);

            HasResults = Results.Count > 0;
            UpdateMatches();

            _logger.LogInformation("Hash computed: source={Source} algorithms={Count} format={Format}",
                fromFile ? "file" : "text", rows.Count, format);

            SetStatus(NotificationType.Success, "L.Hash.Status.Computed", rows.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hash computation cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash computation failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private HashAlgorithmDescriptor[] ResolveDescriptors()
    {
        if (AllAlgorithms)
            return _hashService.Algorithms.ToArray();

        return SelectedAlgorithm is null ? Array.Empty<HashAlgorithmDescriptor>() : new[] { SelectedAlgorithm };
    }

    private string? ResolveFile()
    {
        if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
            return FilePath;

        string? selected = _fileExportService.RequestOpenPath(
            $"{_localizer.Get("L.Common.Filter.AllFiles")}|*.*",
            _localizer.Get("L.Hash.Dialog.SelectFile"));

        if (string.IsNullOrWhiteSpace(selected))
            return null;

        FilePath = selected;
        return selected;
    }

    private static HashResultRow BuildRow(HashAlgorithmDescriptor descriptor, byte[] digest, HashOutputFormat format)
    {
        try
        {
            string display = format switch
            {
                HashOutputFormat.HexLowercase => HexFormatting.ToHexLower(digest),
                HashOutputFormat.Base64 => Convert.ToBase64String(digest),
                _ => HexFormatting.ToHexUpper(digest)
            };

            return new HashResultRow(
                descriptor.DisplayName,
                display,
                HexFormatting.ToHexUpper(digest),
                descriptor.IsWeak,
                descriptor.HexLength);
        }
        finally
        {
            // Özet baytları satırda tutulmaz; iş bittiğinde sıfırlanır.
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    /// <summary>
    /// Girilen beklenen özeti her satırla karşılaştırır. Karşılaştırma her zaman hex üzerinden yapılır;
    /// boşluk, iki nokta ve tire gibi ayırıcılar yok sayılır.
    /// </summary>
    private void UpdateMatches()
    {
        string expected = HexFormatting.NormalizeHex(ExpectedDigest);

        foreach (HashResultRow row in Results)
        {
            if (expected.Length == 0)
            {
                row.Match = HashMatchState.NotChecked;
                row.MatchText = string.Empty;
                continue;
            }

            if (expected.Length != row.HexLength || !IsHex(expected))
            {
                row.Match = HashMatchState.NotComparable;
                row.MatchText = _localizer.Get("L.Hash.Match.NotComparable");
                continue;
            }

            bool isMatch = string.Equals(expected, row.CanonicalHex, StringComparison.OrdinalIgnoreCase);
            row.Match = isMatch ? HashMatchState.Matched : HashMatchState.Mismatched;
            row.MatchText = _localizer.Get(isMatch ? "L.Hash.Match.Matched" : "L.Hash.Match.Mismatched");
        }
    }

    private static bool IsHex(string value)
    {
        foreach (char character in value)
        {
            if (!HexDigits.TryParse(character, out _))
                return false;
        }

        return value.Length > 0;
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Results.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Hash.Validation.NothingToCopy");
            return;
        }

        try
        {
            string payload = Results.Count == 1
                ? Results[0].Digest
                : string.Join(Environment.NewLine, Results.Select(row => $"{row.AlgorithmName}: {row.Digest}"));

            _fileExportService.CopyToClipboard(payload);
            SetStatus(NotificationType.Success, "L.Hash.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (Results.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Hash.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            string payload = string.Join(Environment.NewLine, Results.Select(row => $"{row.AlgorithmName}: {row.Digest}"));

            bool saved = await _fileExportService.ExportTextAsync(
                payload,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "hash-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Hash.Status.Saved" : "L.Hash.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hash output could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelRunningOperations();

        InputText = string.Empty;
        OutputTextClear();
        ExpectedDigest = string.Empty;
        SetStatus(NotificationType.Info, "L.Hash.Status.Cleared");
    }

    private void OutputTextClear()
    {
        Results.Clear();
        HasResults = false;
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(ComputeTextCancelCommand);
        CancelIfRunning(ComputeFileCancelCommand);
    }

    private static void CancelIfRunning(System.Windows.Input.ICommand? command)
    {
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("Hash {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Hash {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
