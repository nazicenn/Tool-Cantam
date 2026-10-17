using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// IP Info sayfası. IP doğrulama, HTTP timeout ve iptal servis tarafından yönetilir.
/// </summary>
public partial class IpInfoPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.IpInfoViewModel>
{
    public ViewModels.IpInfoViewModel ViewModel { get; }

    public IpInfoPage(ViewModels.IpInfoViewModel viewModel)
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