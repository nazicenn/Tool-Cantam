using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class JsonMinifierPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.JsonMinifierViewModel>
{
    public ViewModels.JsonMinifierViewModel ViewModel { get; }
    public JsonMinifierPage(ViewModels.JsonMinifierViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}