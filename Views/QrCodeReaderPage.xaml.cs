using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class QrCodeReaderPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.QrCodeReaderViewModel>
{
    public ViewModels.QrCodeReaderViewModel ViewModel { get; }

    public QrCodeReaderPage(ViewModels.QrCodeReaderViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}