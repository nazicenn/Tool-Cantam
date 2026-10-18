using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class NsLookupPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.NsLookupViewModel>
{
    public ViewModels.NsLookupViewModel ViewModel { get; }
    public NsLookupPage(ViewModels.NsLookupViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}