using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class Rot13Page : UserControl
{
    public Rot13Page(Rot13ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}