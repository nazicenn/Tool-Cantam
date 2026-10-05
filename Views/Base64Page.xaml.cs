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
    }

    private void Clear_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ViewModel.InputText = string.Empty;
        ViewModel.OutputText = string.Empty;
        ViewModel.FileStatus = string.Empty;
    }

    private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
    {

    }
}