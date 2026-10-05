using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class PasswordPage : UserControl
{
    public PasswordPage(PasswordViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Callback'i bağla - Clear butonuna basılınca PasswordBox temizlenecek
        viewModel.OnClearRequested = () =>
        {
            TxtTestPassword.Password = string.Empty;
        };
    }

    private void TxtTestPassword_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PasswordViewModel vm)
        {
            vm.TestPassword = TxtTestPassword.Password;
        }
    }
}