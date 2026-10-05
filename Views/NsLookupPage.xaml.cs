// Views/NsLookupPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class NsLookupPage : UserControl
{
    public NsLookupPage(NsLookupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}