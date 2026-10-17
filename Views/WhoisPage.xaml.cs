using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// WHOIS Lookup sayfası. Domain doğrulama, TCP bağlantı ve zaman aşımı servis tarafından yönetilir.
/// </summary>
public partial class WhoisPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.WhoisViewModel>
{
    public ViewModels.WhoisViewModel ViewModel { get; }

    public WhoisPage(ViewModels.WhoisViewModel viewModel)
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