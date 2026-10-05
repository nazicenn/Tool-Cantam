using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class HashIdentifierPage : UserControl
{
    public HashIdentifierPage(HashIdentifierViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}