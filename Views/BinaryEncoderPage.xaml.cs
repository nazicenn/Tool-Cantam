using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class BinaryEncoderPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.BinaryEncoderViewModel>
{
    public ViewModels.BinaryEncoderViewModel ViewModel { get; }

    public BinaryEncoderPage(ViewModels.BinaryEncoderViewModel viewModel)
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
