using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class MacLookupPage : UserControl
{
    public MacLookupPage(MacLookupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}