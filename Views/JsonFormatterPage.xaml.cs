using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class JsonFormatterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.JsonFormatterViewModel>
{
    public ViewModels.JsonFormatterViewModel ViewModel { get; }

    public JsonFormatterPage(ViewModels.JsonFormatterViewModel viewModel)
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