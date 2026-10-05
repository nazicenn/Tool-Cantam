// Views/PemViewerPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class PemViewerPage : UserControl
{
    public PemViewerPage(PemViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}