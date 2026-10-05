// Views/CssGradientGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class CssGradientGeneratorPage : UserControl
{
    public CssGradientGeneratorPage(CssGradientGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}