using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class LoremIpsumPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.LoremIpsumViewModel>
{
    public ViewModels.LoremIpsumViewModel ViewModel { get; }
    public LoremIpsumPage(ViewModels.LoremIpsumViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}