using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class LoremIpsumPage : UserControl
{
    public LoremIpsumPage(LoremIpsumViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}