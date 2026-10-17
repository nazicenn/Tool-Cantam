using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// DNS Lookup sayfası. Sorgu async, iptal edilebilir; sınırlar servis tarafından uygulanır.
/// </summary>
public partial class DnsPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.DnsViewModel>
{
    public ViewModels.DnsViewModel ViewModel { get; }

    public DnsPage(ViewModels.DnsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        ViewModel.RequestTargetFocus += FocusTarget;
    }

    private void FocusTarget()
    {
        if (!IsLoaded) return;
        TargetTextBox.Focus();
        TargetTextBox.SelectAll();
    }
}