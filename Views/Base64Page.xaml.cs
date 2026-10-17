using System.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class Base64Page : System.Windows.Controls.UserControl, INavigableView<ViewModels.Base64ViewModel>
{
    public ViewModels.Base64ViewModel ViewModel { get; }

    public Base64Page(ViewModels.Base64ViewModel viewModel)
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
