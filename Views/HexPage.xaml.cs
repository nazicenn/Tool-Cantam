using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class HexPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HexViewModel>
{
    public ViewModels.HexViewModel ViewModel { get; }

    public HexPage(ViewModels.HexViewModel viewModel)
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
