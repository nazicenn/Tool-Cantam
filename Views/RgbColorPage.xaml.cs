using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class RgbColorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.RgbColorViewModel>
{
    public ViewModels.RgbColorViewModel ViewModel { get; }
    public RgbColorPage(ViewModels.RgbColorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}