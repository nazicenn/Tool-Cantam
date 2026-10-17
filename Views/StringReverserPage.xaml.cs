using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class StringReverserPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.StringReverserViewModel>
{
    public ViewModels.StringReverserViewModel ViewModel { get; }

    public StringReverserPage(ViewModels.StringReverserViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        ViewModel.RequestInputFocus += FocusInput;
    }

    private void FocusInput()
    {
        if (!IsLoaded) return;
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }
}