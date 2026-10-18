using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class AesEncryptPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.AesEncryptViewModel>
{
    public ViewModels.AesEncryptViewModel ViewModel { get; }
    public AesEncryptPage(ViewModels.AesEncryptViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}