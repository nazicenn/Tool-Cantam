using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class CertificateDecoderPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CertificateDecoderViewModel>
{
    public ViewModels.CertificateDecoderViewModel ViewModel { get; }
    public CertificateDecoderPage(ViewModels.CertificateDecoderViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}