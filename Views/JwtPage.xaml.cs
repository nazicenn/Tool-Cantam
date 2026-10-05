using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class JwtPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.JwtViewModel>
{
    public ViewModels.JwtViewModel ViewModel { get; }
    
    public JwtPage(ViewModels.JwtViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
    {

    }
}

