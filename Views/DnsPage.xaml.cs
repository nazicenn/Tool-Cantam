using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class DnsPage : UserControl
{
    public DnsPage(DnsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}