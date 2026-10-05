using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class CaseConverterPage : UserControl
{
    public CaseConverterPage(CaseConverterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}