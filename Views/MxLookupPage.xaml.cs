// Views/MxLookupPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class MxLookupPage : UserControl
{
    public MxLookupPage(MxLookupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}