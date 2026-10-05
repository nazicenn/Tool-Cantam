using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class SubdomainPage : UserControl
{
    public SubdomainPage(SubdomainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SubdomainsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
}