using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class PwnedPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.PwnedViewModel>
{
    public ViewModels.PwnedViewModel ViewModel { get; }
    public PwnedPage(ViewModels.PwnedViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}