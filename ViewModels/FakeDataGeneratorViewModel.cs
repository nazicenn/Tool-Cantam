using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class FakeDataGeneratorViewModel : ObservableObject
{
    private readonly IFakeDataGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<FakeDataGeneratorViewModel> _logger;

    [ObservableProperty] private int _count = 5;
    [ObservableProperty] private bool _includeName = true;
    [ObservableProperty] private bool _includeEmail = true;
    [ObservableProperty] private bool _includePhone = true;
    [ObservableProperty] private bool _includeAddress = true;
    [ObservableProperty] private bool _includeCompany = true;
    [ObservableProperty] private string _output = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public FakeDataGeneratorViewModel(IFakeDataGeneratorService service, IFileExportService fe, ILocalizerService loc, ILogger<FakeDataGeneratorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.FakeDataGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.FakeDataGenerator.Limits", _service.MinCount, _service.MaxCount);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var opts = new FakeDataOptions(Count, IncludeName, IncludeEmail, IncludePhone, IncludeAddress, IncludeCompany);
            var r = await _service.GenerateAsync(opts, ct);
            if (!r.IsSuccess) { ReportFailure(r, "gen"); Output = string.Empty; return; }
            Output = r.Value!.Json;
            _logger.LogInformation("Fake data: {Count} records, {Fields} fields", r.Value.RecordCount, r.Value.Fields.Count);
            SetStatus(NotificationType.Success, "L.FakeDataGenerator.Status.Generated", r.Value.RecordCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Fake data gen failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(Output)) { SetStatus(NotificationType.Warning, "L.FakeDataGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(Output); SetStatus(NotificationType.Success, "L.FakeDataGenerator.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(Output)) { SetStatus(NotificationType.Warning, "L.FakeDataGenerator.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.FakeDataGenerator.Filter.Json"), ".json", "fakedata");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, Output); SetStatus(NotificationType.Success, "L.FakeDataGenerator.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void Clear() { Output = string.Empty; SetStatus(NotificationType.Info, "L.FakeDataGenerator.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("FakeDataGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}