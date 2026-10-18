using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class UnixTimestampPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.UnixTimestampViewModel>
{
    public ViewModels.UnixTimestampViewModel ViewModel { get; }

    public UnixTimestampPage(ViewModels.UnixTimestampViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}