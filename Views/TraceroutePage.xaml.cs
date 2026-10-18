using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class TraceroutePage : System.Windows.Controls.UserControl, INavigableView<ViewModels.TracerouteViewModel>
{
    public ViewModels.TracerouteViewModel ViewModel { get; }
    public TraceroutePage(ViewModels.TracerouteViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}