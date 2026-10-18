using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class FindReplacePage : System.Windows.Controls.UserControl, INavigableView<ViewModels.FindReplaceViewModel>
{
    public ViewModels.FindReplaceViewModel ViewModel { get; }

    public FindReplacePage(ViewModels.FindReplaceViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}