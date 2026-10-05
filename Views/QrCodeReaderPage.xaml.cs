// Views/QrCodeReaderPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class QrCodeReaderPage : UserControl
{
    public QrCodeReaderPage(QrCodeReaderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}