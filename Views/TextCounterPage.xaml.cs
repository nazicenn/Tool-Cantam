using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class TextCounterPage : UserControl
{
    public TextCounterPage(TextCounterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}