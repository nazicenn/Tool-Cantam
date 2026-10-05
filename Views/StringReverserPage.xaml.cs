using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class StringReverserPage : UserControl
{
    public StringReverserPage(StringReverserViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}