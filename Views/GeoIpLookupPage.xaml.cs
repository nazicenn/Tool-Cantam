using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class GeoIpLookupPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.GeoIpLookupViewModel>
{
    public ViewModels.GeoIpLookupViewModel ViewModel { get; }
    public GeoIpLookupPage(ViewModels.GeoIpLookupViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}