using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class XmlFormatterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.XmlFormatterViewModel>
{
    public ViewModels.XmlFormatterViewModel ViewModel { get; }
    public XmlFormatterPage(ViewModels.XmlFormatterViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}