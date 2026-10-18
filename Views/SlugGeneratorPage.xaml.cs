using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class SlugGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.SlugGeneratorViewModel>
{
    public ViewModels.SlugGeneratorViewModel ViewModel { get; }

    public SlugGeneratorPage(ViewModels.SlugGeneratorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}