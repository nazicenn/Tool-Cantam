using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// Hash araç sayfası. Dosya seçimi sürükle-bırak veya tıklama ile yapılır
/// (<see cref="Helpers.FileDragDropBehavior"/>).
/// </summary>
public partial class HashPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HashViewModel>
{
    public ViewModels.HashViewModel ViewModel { get; }

    public HashPage(ViewModels.HashViewModel viewModel)
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
