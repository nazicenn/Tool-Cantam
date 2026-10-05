using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// TCP port tarama servis arayüzü.
/// Referans: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient
/// </summary>
public interface IPortScannerService
{
    Task<List<int>> ScanPortsAsync(string host, int startPort, int endPort, int timeoutMs = 1000, CancellationToken cancellationToken = default);
}
