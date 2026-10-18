using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class RandomStringGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.RandomStringGeneratorViewModel>
{
    public ViewModels.RandomStringGeneratorViewModel ViewModel { get; }

    public RandomStringGeneratorPage(ViewModels.RandomStringGeneratorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}