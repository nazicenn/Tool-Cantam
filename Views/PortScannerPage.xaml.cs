using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// Port tarayıcı sayfası. Tarama uzunluğu, eşzamanlı bağlantı sayısı ve zaman aşımı sınırları
/// servis tarafından uygulanır; sayfa bu sınırları kullanıcıya gösterir.
/// </summary>
public partial class PortScannerPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.PortScannerViewModel>
{
    public ViewModels.PortScannerViewModel ViewModel { get; }

    public PortScannerPage(ViewModels.PortScannerViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        ViewModel.RequestTargetFocus += FocusTarget;
    }

    private void FocusTarget()
    {
        if (!IsLoaded)
            return;

        TargetTextBox.Focus();
        TargetTextBox.SelectAll();
    }
}
