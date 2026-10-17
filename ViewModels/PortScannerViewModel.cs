using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Açık portun arayüzde gösterilecek hâli.</summary>
public sealed record PortScanRow(int Port, string ServiceText);

/// <summary>
/// Port Scanner aracının sunum katmanı. İş mantığı <see cref="IPortScannerService"/> içindedir.
/// <para>
/// Tarama uzunluğu ve bağlantı sayısı servis tarafından sınırlanır; araç kullanıcıya bu sınırı
/// açıkça gösterir, ilerleme bildirir ve iptal edilebilir.
/// </para>
/// </summary>
public partial class PortScannerViewModel : ObservableObject
{
    private readonly IPortScannerService _portScannerService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<PortScannerViewModel> _logger;

    [ObservableProperty] private string _targetHost = "127.0.0.1";
    [ObservableProperty] private int _startPort = 1;
    [ObservableProperty] private int _endPort = 1024;
    [ObservableProperty] private int _selectedTimeoutMs = 1000;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    /// <summary>Hedef alanı eksik/geçersiz olduğunda görünüm oraya odaklanır.</summary>
    public event Action? RequestTargetFocus;

    public PortScannerViewModel(
        IPortScannerService portScannerService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<PortScannerViewModel> logger)
    {
        _portScannerService = portScannerService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.PortScan.Status.Ready");
        ProgressText = _localizer.Format("L.PortScan.Progress.Initial", DefaultPortCount);
    }

    /// <summary>Açık bulunan portlar.</summary>
    public ObservableCollection<PortScanRow> OpenPorts { get; } = new();

    /// <summary>Seçilebilir bağlantı zaman aşımları (milisaniye).</summary>
    public IReadOnlyList<int> TimeoutOptions { get; } = new[] { 250, 500, 1000, 2000, 5000 };

    /// <summary>Tek taramada izin verilen en fazla port sayısı.</summary>
    public int MaxPortsPerScan => _portScannerService.MaxPortsPerScan;

    /// <summary>Arayüzde gösterilen sınır bilgisi.</summary>
    public string LimitsText => _localizer.Format(
        "L.PortScan.Limits", _portScannerService.MaxPortsPerScan, _portScannerService.MaxConcurrency, _portScannerService.MaxTimeoutMs);

    private int DefaultPortCount => EndPort >= StartPort ? EndPort - StartPort + 1 : 0;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        string target = TargetHost?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.PortScan.Validation.InvalidTarget");
            return;
        }

        if (StartPort is < 1 or > 65535 || EndPort is < 1 or > 65535 || StartPort > EndPort)
        {
            SetStatus(NotificationType.Warning, "L.PortScan.Validation.InvalidRange");
            return;
        }

        int portCount = EndPort - StartPort + 1;

        if (portCount > MaxPortsPerScan)
        {
            SetStatus(NotificationType.Warning, "L.PortScan.Validation.RangeTooLarge", portCount, MaxPortsPerScan);
            return;
        }

        IsScanning = true;
        OpenPorts.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        ProgressPercent = 0;
        ProgressText = _localizer.Format("L.PortScan.Progress.Scanning", 0, portCount, 0);

        // Progress<T> bildirimleri oluşturulduğu bağlama (UI iş parçacığı) geri gönderir.
        var progress = new Progress<PortScanProgress>(OnProgress);

        try
        {
            SetStatus(NotificationType.Info, "L.PortScan.Status.Scanning", target, StartPort, EndPort);

            ToolResult<PortScanReport> result = await _portScannerService.ScanAsync(
                target, StartPort, EndPort, SelectedTimeoutMs, progress, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "port-scan");
                return;
            }

            PortScanReport report = result.Value!;

            foreach (PortScanEntry entry in report.OpenPorts)
                OpenPorts.Add(new PortScanRow(entry.Port, entry.ServiceName ?? string.Empty));

            HasResults = OpenPorts.Count > 0;
            ProgressPercent = 100;
            ProgressText = _localizer.Format("L.PortScan.Progress.Scanning", report.Scanned, report.Scanned, report.OpenCount);
            SummaryText = _localizer.Format(
                "L.PortScan.Status.Summary",
                report.Address.ToString(),
                report.Scanned,
                report.OpenCount,
                report.ClosedCount,
                report.FilteredCount,
                report.Elapsed.TotalSeconds);

            // Hedef, aralık ve sayaçlar loglanır; port başına satır üretilmez.
            _logger.LogInformation(
                "Port scan finished: target={Target} address={Address} range={Start}-{End} scanned={Scanned} open={Open} closed={Closed} filtered={Filtered} elapsedMs={Elapsed}",
                report.Target, report.Address, report.StartPort, report.EndPort,
                report.Scanned, report.OpenCount, report.ClosedCount, report.FilteredCount, report.Elapsed.TotalMilliseconds);

            if (report.OpenCount == 0)
            {
                SetStatus(NotificationType.Info, "L.PortScan.Status.NoOpenPorts", report.Scanned);
                return;
            }

            SetStatus(NotificationType.Success, "L.PortScan.Status.Finished", report.OpenCount, report.Scanned);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Port scan cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            ProgressText = _localizer.Get("L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Port scan failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void OnProgress(PortScanProgress progress)
    {
        ProgressPercent = progress.Total > 0 ? progress.Scanned * 100.0 / progress.Total : 0;
        ProgressText = _localizer.Format("L.PortScan.Progress.Scanning", progress.Scanned, progress.Total, progress.OpenCount);
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (OpenPorts.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.PortScan.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.PortScan.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Port scan result could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (OpenPorts.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.PortScan.Validation.NothingToSave");
            return;
        }

        IsScanning = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt|{_localizer.Get("L.PortScan.Filter.Csv")}|*.csv",
                ".txt",
                "port-scan");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.PortScan.Status.Saved" : "L.PortScan.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Port scan result could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelRunningOperations();

        OpenPorts.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        ProgressPercent = 0;
        ProgressText = _localizer.Format("L.PortScan.Progress.Initial", DefaultPortCount);
        SetStatus(NotificationType.Info, "L.PortScan.Status.Cleared");
    }

    private string BuildReport()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"{_localizer.Get("L.PortScan.Column.Port")}\t{_localizer.Get("L.PortScan.Column.Service")}");

        foreach (PortScanRow row in OpenPorts)
            builder.AppendLine($"{row.Port}\t{row.ServiceText}");

        return builder.ToString().TrimEnd();
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(ScanCancelCommand);
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
            _logger.LogInformation("Port scan {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        // Hedef/aralık hataları kullanıcı girdisidir; ayrıntı loglanır, kullanıcı yerelleştirilmiş mesajı görür.
        _logger.LogWarning("Port scan {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
