using System.Net;

namespace tool_cantam.Core.Models;

/// <summary>Açık bulunan tek bir port.</summary>
public sealed record PortScanEntry(int Port, string? ServiceName);

/// <summary>Tarama ilerlemesi; arayüz çubuğunu besler.</summary>
public sealed record PortScanProgress(int Scanned, int Total, int OpenCount);

/// <summary>
/// Bir tarama oturumunun sonucu. Açık portlar listelenir; kapalı ve filtrelenen portlar yalnızca
/// sayı olarak raporlanır (binlerce satır üretmemek için).
/// </summary>
public sealed record PortScanReport(
    string Target,
    IPAddress Address,
    int StartPort,
    int EndPort,
    int Scanned,
    int OpenCount,
    int ClosedCount,
    int FilteredCount,
    TimeSpan Elapsed,
    IReadOnlyList<PortScanEntry> OpenPorts);
