using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class UnicodeConverterPage : UserControl
{
    public UnicodeConverterPage(UnicodeConverterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}