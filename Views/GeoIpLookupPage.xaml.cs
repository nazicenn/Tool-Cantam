// Views/GeoIpLookupPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class GeoIpLookupPage : UserControl
{
    public GeoIpLookupPage(GeoIpLookupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}