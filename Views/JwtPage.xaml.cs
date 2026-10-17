using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class JwtPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.JwtViewModel>
{
    public ViewModels.JwtViewModel ViewModel { get; }

    public JwtPage(ViewModels.JwtViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        ViewModel.RequestInputFocus += FocusInput;
    }

    private void FocusInput()
    {
        if (!IsLoaded) return;
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }
}