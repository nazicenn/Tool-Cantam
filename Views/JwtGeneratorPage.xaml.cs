using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class JwtGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.JwtGeneratorViewModel>
{
    public ViewModels.JwtGeneratorViewModel ViewModel { get; }
    public JwtGeneratorPage(ViewModels.JwtGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}