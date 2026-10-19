using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class RestApiClientPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.RestApiClientViewModel>
{
    public ViewModels.RestApiClientViewModel ViewModel { get; }
    public RestApiClientPage(ViewModels.RestApiClientViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}