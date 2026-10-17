using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class UrlPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.UrlViewModel>
{
    public ViewModels.UrlViewModel ViewModel { get; }

    public UrlPage(ViewModels.UrlViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        // Girdi eksikse görünüm odaklanmayı yönetir; ViewModel'de UI bağımlılığı yoktur.
        ViewModel.RequestInputFocus += FocusInput;
    }

    private void FocusInput()
    {
        if (!IsLoaded)
            return;

        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }
}
