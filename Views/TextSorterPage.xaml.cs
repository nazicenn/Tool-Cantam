// Views/TextSorterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class TextSorterPage : UserControl
{
    public TextSorterPage(TextSorterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}