// Views/CertificateDecoderPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class CertificateDecoderPage : UserControl
{
    public CertificateDecoderPage(CertificateDecoderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}