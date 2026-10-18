using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class YamlFormatterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.YamlFormatterViewModel>
{
    public ViewModels.YamlFormatterViewModel ViewModel { get; }
    public YamlFormatterPage(ViewModels.YamlFormatterViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}