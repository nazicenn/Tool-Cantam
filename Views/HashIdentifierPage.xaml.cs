using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class HashIdentifierPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HashIdentifierViewModel>
{
    public ViewModels.HashIdentifierViewModel ViewModel { get; }

    public HashIdentifierPage(ViewModels.HashIdentifierViewModel viewModel)
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
