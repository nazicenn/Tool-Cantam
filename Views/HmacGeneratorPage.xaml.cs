using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class HmacGeneratorPage : UserControl
{
    public HmacGeneratorPage(HmacGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}