using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class CronParserPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CronParserViewModel>
{
    public ViewModels.CronParserViewModel ViewModel { get; }
    public CronParserPage(ViewModels.CronParserViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}