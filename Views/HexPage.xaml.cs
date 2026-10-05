using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class HexPage : UserControl
{
    public HexPage(HexViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}