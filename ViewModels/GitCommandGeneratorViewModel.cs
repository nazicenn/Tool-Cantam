using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class GitCommandGeneratorViewModel : ObservableObject
{
    private readonly IGitCommandGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<GitCommandGeneratorViewModel> _logger;

    [ObservableProperty] private GitCommandCategory _category = GitCommandCategory.Commit;
    [ObservableProperty] private string _argument1 = string.Empty;
    [ObservableProperty] private string _argument2 = string.Empty;
    [ObservableProperty] private bool _flag1;
    [ObservableProperty] private bool _flag2;
    [ObservableProperty] private string _command = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<GitCommandCategory> Categories { get; } = new();

    public IAsyncRelayCommand GenerateCommand { get; }
    public IRelayCommand CopyCommand { get; }
    public IRelayCommand LoadSampleCommand { get; }
    public IRelayCommand ClearCommand { get; }

    public GitCommandGeneratorViewModel(
        IGitCommandGeneratorService service,
        IFileExportService fe,
        ILocalizerService loc,
        ILogger<GitCommandGeneratorViewModel> log)
    {
        _service = service;
        _fileExportService = fe;
        _localizer = loc;
        _logger = log;
        StatusText = _localizer.Get("L.GitCommand.Status.Ready");
        foreach (var c in _service.Categories) Categories.Add(c);

        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        CopyCommand = new RelayCommand(Copy);
        LoadSampleCommand = new RelayCommand(LoadSample);
        ClearCommand = new RelayCommand(Clear);
    }

    public string LimitsText => _localizer.Format("L.GitCommand.Limits", _service.MaxInputLength);

    private async Task GenerateAsync()
    {
        IsBusy = true;
        try
        {
            var opts = new GitCommandOptions(Category, Argument1, Argument2, Flag1, Flag2);
            var r = await _service.GenerateAsync(opts, CancellationToken.None);
            if (!r.IsSuccess)
            {
                ReportFailure(r, "generate");
                Command = string.Empty;
                Description = string.Empty;
                return;
            }
            Command = r.Value!.Command;
            Description = r.Value.Description;
            SetStatus(NotificationType.Success, "L.GitCommand.Status.Generated", r.Value.Category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git command failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Copy()
    {
        if (string.IsNullOrWhiteSpace(Command))
        {
            SetStatus(NotificationType.Warning, "L.GitCommand.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(Command);
            SetStatus(NotificationType.Success, "L.GitCommand.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    private void LoadSample()
    {
        Category = GitCommandCategory.Commit;
        Argument1 = "Initial commit";
        Flag1 = true;
        Flag2 = false;
        GenerateCommand.Execute(null);
    }

    private void Clear()
    {
        Argument1 = string.Empty;
        Argument2 = string.Empty;
        Flag1 = false;
        Flag2 = false;
        Command = string.Empty;
        Description = string.Empty;
        SetStatus(NotificationType.Info, "L.GitCommand.Status.Cleared");
    }

    private void SetStatus(NotificationType t, string k, params object?[] a)
    {
        StatusType = t;
        StatusText = _localizer.Format(k, a);
    }

    private void ReportFailure<T>(ToolResult<T> r, string op)
    {
        _logger.LogWarning("GitCommand {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode);
        SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}