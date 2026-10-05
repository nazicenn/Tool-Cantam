namespace tool_cantam.Core.Contracts;

public interface IToolNavigationService
{
    void SetFrame(System.Windows.Controls.Frame frame);
    void Navigate<TViewModel>() where TViewModel : class;
    void GoBack();
    bool CanGoBack { get; }
}