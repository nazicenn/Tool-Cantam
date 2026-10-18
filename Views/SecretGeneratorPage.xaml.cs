using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class SecretGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.SecretGeneratorViewModel>
{
    public ViewModels.SecretGeneratorViewModel ViewModel { get; }
    public SecretGeneratorPage(ViewModels.SecretGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}