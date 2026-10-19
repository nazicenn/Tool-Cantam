using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class UserAgentParserPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.UserAgentParserViewModel>
{
    public ViewModels.UserAgentParserViewModel ViewModel { get; }
    public UserAgentParserPage(ViewModels.UserAgentParserViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}