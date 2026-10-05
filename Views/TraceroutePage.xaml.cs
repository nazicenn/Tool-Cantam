// Views/TraceroutePage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class TraceroutePage : UserControl
{
    public TraceroutePage(TracerouteViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}