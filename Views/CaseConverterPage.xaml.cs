using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class CaseConverterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CaseConverterViewModel>
{
    public ViewModels.CaseConverterViewModel ViewModel { get; }

    public CaseConverterPage(ViewModels.CaseConverterViewModel viewModel)
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