using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class HttpRequestTesterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HttpRequestTesterViewModel>
{
    public ViewModels.HttpRequestTesterViewModel ViewModel { get; }
    public HttpRequestTesterPage(ViewModels.HttpRequestTesterViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}