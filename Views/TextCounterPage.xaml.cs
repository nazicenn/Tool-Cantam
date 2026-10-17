using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class TextCounterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.TextCounterViewModel>
{
    public ViewModels.TextCounterViewModel ViewModel { get; }

    public TextCounterPage(ViewModels.TextCounterViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = this;
    }
}