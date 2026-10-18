using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class QrCodePage : System.Windows.Controls.UserControl, INavigableView<ViewModels.QrCodeViewModel>
{
    public ViewModels.QrCodeViewModel ViewModel { get; }

    public QrCodePage(ViewModels.QrCodeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}