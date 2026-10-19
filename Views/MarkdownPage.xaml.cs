using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class MarkdownPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.MarkdownViewModel>
{
    public ViewModels.MarkdownViewModel ViewModel { get; }
    public MarkdownPage(ViewModels.MarkdownViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}