using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class ReverseDnsPage : UserControl
{
    public ReverseDnsPage(ReverseDnsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}