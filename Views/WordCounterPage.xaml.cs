using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class WordCounterPage : UserControl
{
    public WordCounterPage(WordCounterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}