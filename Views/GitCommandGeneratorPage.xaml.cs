using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class GitCommandGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.GitCommandGeneratorViewModel>
{
    public ViewModels.GitCommandGeneratorViewModel ViewModel { get; }

    public GitCommandGeneratorPage(ViewModels.GitCommandGeneratorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}