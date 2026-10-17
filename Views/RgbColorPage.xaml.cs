using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class RgbColorPage : UserControl
{
    public RgbColorPage(RgbColorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}