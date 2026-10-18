using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class TextSorterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.TextSorterViewModel>
{
    public ViewModels.TextSorterViewModel ViewModel { get; }

    public TextSorterPage(ViewModels.TextSorterViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}