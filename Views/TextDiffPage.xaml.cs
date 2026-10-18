using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class TextDiffPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.TextDiffViewModel>
{
    public ViewModels.TextDiffViewModel ViewModel { get; }

    public TextDiffPage(ViewModels.TextDiffViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}