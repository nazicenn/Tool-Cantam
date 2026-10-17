using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Durum bildirimlerinin tek kanalı. Kabuk (MainWindow) <see cref="StatusChanged"/> olayına abone olur;
/// abone yoksa bildirim sessizce kaybolmaz, loga yazılır.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger) => _logger = logger;

    /// <summary>Durum bildirimi yayınlandığında tetiklenir. Abone: kabuk durum çubuğu.</summary>
    public event Action<string, NotificationType>? StatusChanged;

    public void ShowStatus(string message, NotificationType type = NotificationType.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _logger.LogInformation("Tool status [{Type}]: {Message}", type, message);

        var handler = StatusChanged;
        if (handler is null)
            return;

        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.Invoke(() => handler(message, type));
        else
            handler(message, type);
    }

    public async Task ShowDialogAsync(string message, string title = "Info")
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null)
        {
            _logger.LogWarning("Dialog could not be shown because the application is not running: {Title}", title);
            return;
        }

        await dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }
}
