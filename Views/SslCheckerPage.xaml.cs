using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class SslCheckerPage : UserControl
{
    public SslCheckerPage(SslCheckerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}