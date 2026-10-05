namespace tool_cantam.Services.Interfaces;

public interface INotificationService
{
    void ShowStatus(string message, NotificationType type = NotificationType.Info);
    Task ShowDialogAsync(string message, string title = "Info");
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}