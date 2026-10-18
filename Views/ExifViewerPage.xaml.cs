using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class ExifViewerPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.ExifViewerViewModel>
{
    public ViewModels.ExifViewerViewModel ViewModel { get; }
    public ExifViewerPage(ViewModels.ExifViewerViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}