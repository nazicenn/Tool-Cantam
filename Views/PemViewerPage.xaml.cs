using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class PemViewerPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.PemViewerViewModel>
{
    public ViewModels.PemViewerViewModel ViewModel { get; }
    public PemViewerPage(ViewModels.PemViewerViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}