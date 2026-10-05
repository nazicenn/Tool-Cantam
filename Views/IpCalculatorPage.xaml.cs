// Views/IpCalculatorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class IpCalculatorPage : UserControl
{
    public IpCalculatorPage(IpCalculatorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}