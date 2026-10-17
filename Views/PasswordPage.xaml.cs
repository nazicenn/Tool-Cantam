using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class PasswordPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.PasswordViewModel>
{
    public ViewModels.PasswordViewModel ViewModel { get; }

    public PasswordPage(ViewModels.PasswordViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        ViewModel.OnClearRequested += () =>
        {
            TxtTestPassword.Password = string.Empty;
        };
    }

    private void TxtTestPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.TestPassword = TxtTestPassword.Password;
    }
}