using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class Base64ImageConverterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.Base64ImageConverterViewModel>
{
    public ViewModels.Base64ImageConverterViewModel ViewModel { get; }
    public Base64ImageConverterPage(ViewModels.Base64ImageConverterViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}