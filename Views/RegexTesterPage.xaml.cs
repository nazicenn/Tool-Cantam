using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class RegexTesterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.RegexTesterViewModel>
{
    public ViewModels.RegexTesterViewModel ViewModel { get; }

    public RegexTesterPage(ViewModels.RegexTesterViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}