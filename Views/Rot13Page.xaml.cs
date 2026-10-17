using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class Rot13Page : System.Windows.Controls.UserControl, INavigableView<ViewModels.Rot13ViewModel>
{
    public ViewModels.Rot13ViewModel ViewModel { get; }

    public Rot13Page(ViewModels.Rot13ViewModel viewModel)
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
