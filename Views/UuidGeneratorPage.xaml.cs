using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class UuidGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.UuidGeneratorViewModel>
{
    public ViewModels.UuidGeneratorViewModel ViewModel { get; }

    public UuidGeneratorPage(ViewModels.UuidGeneratorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}