using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class SslCheckerPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.SslCheckerViewModel>
{
    public ViewModels.SslCheckerViewModel ViewModel { get; }

    public SslCheckerPage(ViewModels.SslCheckerViewModel viewModel)
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