using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class MxLookupPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.MxLookupViewModel>
{
    public ViewModels.MxLookupViewModel ViewModel { get; }
    public MxLookupPage(ViewModels.MxLookupViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}