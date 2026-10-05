// Views/FindReplacePage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class FindReplacePage : UserControl
{
    public FindReplacePage(FindReplaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}