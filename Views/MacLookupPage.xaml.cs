using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// MAC Lookup sayfası. MAC doğrulama, HTTP timeout ve iptal servis tarafından yönetilir.
/// </summary>
public partial class MacLookupPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.MacLookupViewModel>
{
    public ViewModels.MacLookupViewModel ViewModel { get; }

    public MacLookupPage(ViewModels.MacLookupViewModel viewModel)
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