using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class AsciiConverterPage : UserControl
{
    public AsciiConverterPage(AsciiConverterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}