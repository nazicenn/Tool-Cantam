using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class RsaKeyGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.RsaKeyGeneratorViewModel>
{
    public ViewModels.RsaKeyGeneratorViewModel ViewModel { get; }
    public RsaKeyGeneratorPage(ViewModels.RsaKeyGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}