using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class BcryptGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.BcryptGeneratorViewModel>
{
    public ViewModels.BcryptGeneratorViewModel ViewModel { get; }
    public BcryptGeneratorPage(ViewModels.BcryptGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}