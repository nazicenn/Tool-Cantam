using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// Reverse DNS sayfası. IP doğrulama, timeout ve iptal servis tarafından yönetilir.
/// </summary>
public partial class ReverseDnsPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.ReverseDnsViewModel>
{
    public ViewModels.ReverseDnsViewModel ViewModel { get; }

    public ReverseDnsPage(ViewModels.ReverseDnsViewModel viewModel)
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