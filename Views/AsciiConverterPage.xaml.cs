using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class AsciiConverterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.AsciiConverterViewModel>
{
    public ViewModels.AsciiConverterViewModel ViewModel { get; }

    public AsciiConverterPage(ViewModels.AsciiConverterViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;

        // Girdi eksikse görünüm odaklanmayı yönetir; ViewModel'de UI bağımlılığı yoktur.
        ViewModel.RequestInputFocus += FocusInput;
    }

    private void FocusInput()
    {
        if (!IsLoaded)
            return;

        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }
}
