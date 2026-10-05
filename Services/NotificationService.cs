using System;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class NotificationService : INotificationService
{
    private readonly Action<string, NotificationType>? _statusUpdateAction;

    public NotificationService(Action<string, NotificationType>? statusUpdateAction = null)
    {
        _statusUpdateAction = statusUpdateAction;
    }

    public void ShowStatus(string message, NotificationType type = NotificationType.Info)
    {
        _statusUpdateAction?.Invoke(message, type);
    }

    public async Task ShowDialogAsync(string message, string title = "Info")
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }
}