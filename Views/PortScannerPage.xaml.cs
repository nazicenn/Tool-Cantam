using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class PortScannerPage : INavigableView<ViewModels.PortScannerViewModel>
{
    public ViewModels.PortScannerViewModel ViewModel { get; }
    public PortScannerPage(ViewModels.PortScannerViewModel viewModel) {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}

