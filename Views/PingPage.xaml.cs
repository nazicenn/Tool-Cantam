using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class PingPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.PingViewModel>
{
    public ViewModels.PingViewModel ViewModel { get; }

    public PingPage(ViewModels.PingViewModel viewModel)
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