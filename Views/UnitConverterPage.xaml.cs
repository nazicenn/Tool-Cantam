using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class UnitConverterPage : UserControl
{
    public UnitConverterPage(UnitConverterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}