using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class IpCalculatorViewModel : ObservableObject
{
    private readonly IIpCalculatorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<IpCalculatorViewModel> _logger;

    [ObservableProperty] private string _ipAddress = "192.168.1.0";
    [ObservableProperty] private int _cidr = 24;
    [ObservableProperty] private string _networkAddress = string.Empty;
    [ObservableProperty] private string _broadcastAddress = string.Empty;
    [ObservableProperty] private string _firstUsable = string.Empty;
    [ObservableProperty] private string _lastUsable = string.Empty;
    [ObservableProperty] private string _subnetMask = string.Empty;
    [ObservableProperty] private string _wildcardMask = string.Empty;
    [ObservableProperty] private long _totalHosts;
    [ObservableProperty] private long _usableHosts;
    [ObservableProperty] private string _binaryIp = string.Empty;
    [ObservableProperty] private string _binaryMask = string.Empty;
    [ObservableProperty] private bool _isPrivate;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public IpCalculatorViewModel(IIpCalculatorService service, IFileExportService fe, ILocalizerService loc, ILogger<IpCalculatorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.IpCalculator.Status.Ready");
    }

    public string LimitsText => _localizer.Get("L.IpCalculator.Limits");

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CalculateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.CalculateAsync(IpAddress, Cidr, ct);
            if (!r.IsSuccess) { ReportFailure(r, "calc"); ClearResults(); return; }
            var v = r.Value!;
            NetworkAddress = v.Network;
            BroadcastAddress = v.Broadcast;
            FirstUsable = v.FirstUsable;
            LastUsable = v.LastUsable;
            SubnetMask = v.SubnetMask;
            WildcardMask = v.WildcardMask;
            TotalHosts = v.TotalHosts;
            UsableHosts = v.UsableHosts;
            BinaryIp = v.BinaryIp;
            BinaryMask = v.BinaryMask;
            IsPrivate = v.IsPrivate;
            _logger.LogInformation("IP calc: {Ip}/{Cidr}", v.IpAddress, v.Cidr);
            SetStatus(NotificationType.Success, "L.IpCalculator.Status.Calculated", v.Network, v.Cidr);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "IP calc failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void LoadSample() { IpAddress = "192.168.1.0"; Cidr = 24; CalculateCommand.Execute(null); }

    [RelayCommand]
    private void CopyResult()
    {
        if (string.IsNullOrWhiteSpace(NetworkAddress)) { SetStatus(NotificationType.Warning, "L.IpCalculator.Validation.NothingToCopy"); return; }
        var text = $"IP: {IpAddress}/{Cidr}\nNetwork: {NetworkAddress}\nBroadcast: {BroadcastAddress}\nSubnet: {SubnetMask}\nWildcard: {WildcardMask}\nFirst: {FirstUsable}\nLast: {LastUsable}\nTotal: {TotalHosts}\nUsable: {UsableHosts}\nPrivate: {IsPrivate}";
        try { _fileExportService.CopyToClipboard(text); SetStatus(NotificationType.Success, "L.IpCalculator.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear() { ClearResults(); SetStatus(NotificationType.Info, "L.IpCalculator.Status.Cleared"); }

    private void ClearResults() { NetworkAddress = string.Empty; BroadcastAddress = string.Empty; FirstUsable = string.Empty; LastUsable = string.Empty; SubnetMask = string.Empty; WildcardMask = string.Empty; TotalHosts = 0; UsableHosts = 0; BinaryIp = string.Empty; BinaryMask = string.Empty; IsPrivate = false; }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("IpCalculator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}