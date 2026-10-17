using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// JSON Formatter aracının sunum katmanı. İş mantığı <see cref="IJsonFormatterService"/> içindedir.
/// Format, minify ve validate işlemleri async, iptal edilebilir.
/// </summary>
public partial class JsonFormatterViewModel : ObservableObject
{
    private readonly IJsonFormatterService _jsonService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<JsonFormatterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _indentSize = 2;

    [ObservableProperty] private int _nodeCount;
    [ObservableProperty] private int _jsonDepth;
    [ObservableProperty] private string _sizeText = "-";

    public event Action? RequestInputFocus;

    public int[] IndentOptions { get; } = { 2, 4 };

    public JsonFormatterViewModel(
        IJsonFormatterService jsonService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<JsonFormatterViewModel> logger)
    {
        _jsonService = jsonService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.JsonFormatter.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.JsonFormatter.Limits", _jsonService.MaxTextLength, _jsonService.MaxDepth);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task FormatAsync(CancellationToken ct) => ProcessInternalAsync("format", ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task MinifyAsync(CancellationToken ct) => ProcessInternalAsync("minify", ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ValidateAsync(CancellationToken ct) => ProcessInternalAsync("validate", ct);

    private async Task ProcessInternalAsync(string operation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.JsonFormatter.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            int indent = IndentSize;

            ToolResult<JsonFormatReport> result = await Task.Run(() =>
            {
                return operation switch
                {
                    "format" => _jsonService.Format(input, indent),
                    "minify" => _jsonService.Minify(input),
                    "validate" => _jsonService.Validate(input),
                    _ => ToolResult<JsonFormatReport>.Fail(
                        ToolErrorKind.Validation, "L.Common.Error.Unexpected", "Unknown operation")
                };
            }, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, operation);
                return;
            }

            var report = result.Value!;

            NodeCount = report.NodeCount;
            JsonDepth = report.MaxDepth;
            SizeText = $"{report.InputBytes} → {report.OutputBytes} B";
            OutputText = report.Output;

            _logger.LogInformation(
                "JSON {Operation} completed: nodes={Nodes} depth={Depth} inputBytes={InputBytes} outputBytes={OutputBytes}",
                operation, report.NodeCount, report.MaxDepth, report.InputBytes, report.OutputBytes);

            string statusKey = operation switch
            {
                "format" => "L.JsonFormatter.Status.Formatted",
                "minify" => "L.JsonFormatter.Status.Minified",
                _ => "L.JsonFormatter.Status.Validated"
            };

            SetStatus(NotificationType.Success, statusKey, report.NodeCount, report.MaxDepth);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("JSON {Operation} cancelled by the user.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON {Operation} failed unexpectedly.", operation);
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.JsonFormatter.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.JsonFormatter.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON output could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.JsonFormatter.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt|JSON (*.json)|*.json",
                ".json",
                "formatted");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.JsonFormatter.Status.Saved" : "L.JsonFormatter.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON output could not be saved.");
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
        OutputText = string.Empty;
        NodeCount = 0;
        JsonDepth = 0;
        SizeText = "-";
        SetStatus(NotificationType.Info, "L.JsonFormatter.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(FormatCancelCommand);
        CancelIfRunning(MinifyCancelCommand);
        CancelIfRunning(ValidateCancelCommand);
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
            _logger.LogInformation("JSON {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("JSON {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}