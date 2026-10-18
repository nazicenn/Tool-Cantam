using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class WordCounterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.WordCounterViewModel>
{
    public ViewModels.WordCounterViewModel ViewModel { get; }
    public WordCounterPage(ViewModels.WordCounterViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}