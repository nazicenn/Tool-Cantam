using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// TCP port tarama servis uygulaması.
/// Güvenlik: IP/hostname validasyonu, paralellik sınırı ve CancellationToken desteği içerir.
/// </summary>
public class PortScannerService : IPortScannerService
{
    /// <summary>Ağ yükünü ve firewall tetiklenmesini önlemek için eşzamanlı bağlantı sınırı.</summary>
    private const int MaxConcurrency = 50;

    public async Task<List<int>> ScanPortsAsync(string host, int startPort, int endPort, int timeoutMs = 1000, CancellationToken cancellationToken = default)
    {
        // 1. Host validasyonu
        ValidateHost(host);

        // 2. Port aralığı validasyonu
        if (startPort < 1 || startPort > 65535)
            throw new ArgumentOutOfRangeException(nameof(startPort), $"Start port must be between 1-65535. Given: {startPort}");
        if (endPort < 1 || endPort > 65535)
            throw new ArgumentOutOfRangeException(nameof(endPort), $"End port must be between 1-65535. Given: {endPort}");
        if (startPort > endPort)
            throw new ArgumentException($"Start port ({startPort}) cannot be greater than end port ({endPort}).");

        var openPorts = new ConcurrentBag<int>();
        var ports = Enumerable.Range(startPort, endPort - startPort + 1);

        // 3. Sınırlı paralellik ile tarama
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrency,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(ports, options, async (port, ct) =>
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port, ct);

                if (await Task.WhenAny(connectTask.AsTask(), Task.Delay(timeoutMs, ct)) == connectTask.AsTask())
                {
                    if (client.Connected) openPorts.Add(port);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Cancellation request must propagate upward
            }
            catch
            {
                // Bağlantı reddedildi / zaman aşımı = port kapalı (beklenen durum)
            }
        }).ConfigureAwait(false);

        return openPorts.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Host adresinin geçerli bir IP veya hostname formatında olduğunu doğrular.
    /// Referans: https://learn.microsoft.com/en-us/dotnet/api/system.uri.checkhostname
    /// </summary>
    private static void ValidateHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        // IP adresini veya hostname formatını kontrol et
        bool isValidIp = IPAddress.TryParse(host, out _);
        bool isValidHostname = Uri.CheckHostName(host) != UriHostNameType.Unknown;

        if (!isValidIp && !isValidHostname)
            throw new ArgumentException($"Invalid target address: '{host}'. Please enter a valid IP address or hostname.");
    }
}
