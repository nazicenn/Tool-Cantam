// Views/ExifViewerPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class ExifViewerPage : UserControl
{
    public ExifViewerPage(ExifViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}