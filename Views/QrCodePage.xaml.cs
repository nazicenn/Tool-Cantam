using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class QrCodePage : UserControl
{
    public QrCodePage(QrCodeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}