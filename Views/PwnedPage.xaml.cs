using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class PwnedPage : UserControl
{
    public PwnedPage(PwnedViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}