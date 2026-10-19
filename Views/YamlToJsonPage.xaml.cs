using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class YamlToJsonPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.YamlToJsonViewModel>
{
    public ViewModels.YamlToJsonViewModel ViewModel { get; }
    public YamlToJsonPage(ViewModels.YamlToJsonViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}