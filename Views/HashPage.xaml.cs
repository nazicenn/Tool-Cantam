using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// Hash araç sayfası. Drag-Drop işlevi FileDragDropBehavior ile sağlanır.
/// </summary>
public partial class HashPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HashViewModel>
{
    public ViewModels.HashViewModel ViewModel { get; }
    public HashPage(ViewModels.HashViewModel viewModel) {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
    {

    }
}
