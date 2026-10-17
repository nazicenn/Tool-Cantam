using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// Subdomain Scanner sayfası. Adım sayısı ve eşzamanlı sorgu sınırları servis tarafından uygulanır;
/// sayfa bu sınırları kullanıcıya gösterir ve iptal edilebilir.
/// </summary>
public partial class SubdomainPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.SubdomainViewModel>
{
    public ViewModels.SubdomainViewModel ViewModel { get; }

    public SubdomainPage(ViewModels.SubdomainViewModel viewModel)
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