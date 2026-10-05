using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class IpInfoPage : UserControl
{
    public IpInfoPage(IpInfoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}