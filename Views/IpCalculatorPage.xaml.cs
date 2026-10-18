using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class IpCalculatorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.IpCalculatorViewModel>
{
    public ViewModels.IpCalculatorViewModel ViewModel { get; }
    public IpCalculatorPage(ViewModels.IpCalculatorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}