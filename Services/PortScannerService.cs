using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using tool_cantam.Core.Models;
using tool_cantam.Core.Net;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// TCP port tarayıcısının tek uygulaması.
/// <para>
/// Kurallar: hedef bir kez çözümlenir ve bağlantılar doğrudan IP adresine yapılır (port sayısı kadar
/// DNS sorgusu yapılmaz); eşzamanlı bağlantı sayısı ve tarama uzunluğu sınırlıdır; her bağlantı
/// kendi zaman aşımına sahiptir ve kullanıcı iptali alt katmana kadar iletilir. Sonuçta açık portlar
/// servis adlarıyla döner, kapalı/filtrelenen portlar yalnızca sayılır.
/// </para>
/// </summary>
public sealed class PortScannerService : IPortScannerService
{
    public const int MaxPortsPerScanLimit = 4096;
    public const int MaxConcurrencyLimit = 50;
    public const int MinTimeoutMsLimit = 100;
    public const int MaxTimeoutMsLimit = 5000;

    /// <summary>İlerleme bildirimleri her bu kadar portta bir gönderilir (arayüzü boğmamak için).</summary>
    private const int ProgressBatchSize = 25;

    public int MaxPortsPerScan => MaxPortsPerScanLimit;

    public int MaxConcurrency => MaxConcurrencyLimit;

    public int MaxTimeoutMs => MaxTimeoutMsLimit;

    public async Task<ToolResult<PortScanReport>> ScanAsync(
        string? target,
        int startPort,
        int endPort,
        int timeoutMs,
        IProgress<PortScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ToolResult<PortScanReport>.Fail(
                ToolErrorKind.Validation, PortScanErrorCodes.InvalidTarget, "Target is empty.");
        }

        if (startPort is < 1 or > 65535 || endPort is < 1 or > 65535)
        {
            return ToolResult<PortScanReport>.Fail(
                ToolErrorKind.Validation, PortScanErrorCodes.InvalidRange,
                $"Ports must be between 1 and 65535 (start={startPort}, end={endPort}).");
        }

        if (startPort > endPort)
        {
            return ToolResult<PortScanReport>.Fail(
                ToolErrorKind.Validation, PortScanErrorCodes.InvalidRange,
                $"Start port ({startPort}) is greater than end port ({endPort}).");
        }

        int portCount = endPort - startPort + 1;

        if (portCount > MaxPortsPerScanLimit)
        {
            return ToolResult<PortScanReport>.Fail(
                ToolErrorKind.Validation, PortScanErrorCodes.RangeTooLarge,
                $"The range contains {portCount} ports; the limit is {MaxPortsPerScanLimit} per scan.");
        }

        string host = target.Trim();
        bool isAddress = IPAddress.TryParse(host, out IPAddress? address);

        if (!isAddress && Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            return ToolResult<PortScanReport>.Fail(
                ToolErrorKind.Validation, PortScanErrorCodes.InvalidTarget,
                $"'{host}' is not a valid IP address or host name.");
        }

        var stopwatch = Stopwatch.StartNew();

        if (!isAddress)
        {
            try
            {
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
                address = PreferIpv4(addresses);
            }
            catch (OperationCanceledException)
            {
                return Cancelled();
            }
            catch (Exception ex) when (ex is SocketException or ArgumentException)
            {
                return ToolResult<PortScanReport>.Fail(
                    ToolErrorKind.NotFound, PortScanErrorCodes.TargetNotFound, ex.Message);
            }

            if (address is null)
            {
                return ToolResult<PortScanReport>.Fail(
                    ToolErrorKind.NotFound, PortScanErrorCodes.TargetNotFound, $"No address found for '{host}'.");
            }
        }

        TimeSpan timeout = TimeSpan.FromMilliseconds(
            Math.Clamp(timeoutMs, MinTimeoutMsLimit, MaxTimeoutMsLimit));

        var openPorts = new List<PortScanEntry>();
        int scanned = 0;
        int openCount = 0;
        int closedCount = 0;
        int filteredCount = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrencyLimit,
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(startPort, portCount), options, async (port, token) =>
            {
                ProbeOutcome outcome = await ProbeAsync(address!, port, timeout, token).ConfigureAwait(false);

                switch (outcome)
                {
                    case ProbeOutcome.Open:
                        lock (openPorts)
                            openPorts.Add(new PortScanEntry(port, WellKnownPorts.GetServiceName(port)));
                        Interlocked.Increment(ref openCount);
                        break;

                    case ProbeOutcome.Filtered:
                        Interlocked.Increment(ref filteredCount);
                        break;

                    default:
                        Interlocked.Increment(ref closedCount);
                        break;
                }

                int done = Interlocked.Increment(ref scanned);

                if (progress is not null && (done % ProgressBatchSize == 0 || done == portCount))
                {
                    progress.Report(new PortScanProgress(done, portCount, Volatile.Read(ref openCount)));
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Cancelled();
        }
        catch (Exception ex)
        {
            return ToolResult<PortScanReport>.Fail(
                ToolErrorKind.Unexpected, PortScanErrorCodes.Unexpected, ex.ToString());
        }

        stopwatch.Stop();
        openPorts.Sort((left, right) => left.Port.CompareTo(right.Port));

        return ToolResult<PortScanReport>.Ok(new PortScanReport(
            host,
            address!,
            startPort,
            endPort,
            scanned,
            openCount,
            closedCount,
            filteredCount,
            stopwatch.Elapsed,
            openPorts));
    }

    /// <summary>
    /// Tek bir porta bağlanmayı dener. Kendi zaman aşımı bağlantıya bağlanır; böylece zaman aşımında
    /// yarım kalan bir bağlantı denemesi kalmaz. Kullanıcı iptali üst katmana iletilir.
    /// </summary>
    private static async Task<ProbeOutcome> ProbeAsync(IPAddress address, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var client = new TcpClient(address.AddressFamily);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await client.ConnectAsync(address, port, timeoutSource.Token).ConfigureAwait(false);
            return ProbeOutcome.Open;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Kendi zaman aşımımız: port yanıt vermedi.
            return ProbeOutcome.Filtered;
        }
        catch (SocketException ex)
        {
            return ex.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => ProbeOutcome.Closed,
                SocketError.TimedOut => ProbeOutcome.Filtered,
                SocketError.HostUnreachable or SocketError.NetworkUnreachable => ProbeOutcome.Filtered,
                SocketError.ConnectionReset => ProbeOutcome.Closed,
                _ => ProbeOutcome.Closed
            };
        }
    }

    private static IPAddress? PreferIpv4(IPAddress[] addresses)
    {
        foreach (IPAddress candidate in addresses)
        {
            if (candidate.AddressFamily == AddressFamily.InterNetwork)
                return candidate;
        }

        return addresses.Length > 0 ? addresses[0] : null;
    }

    private static ToolResult<PortScanReport> Cancelled() =>
        ToolResult<PortScanReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Scan cancelled by the user.");

    private enum ProbeOutcome
    {
        Open,
        Closed,
        Filtered
    }
}
