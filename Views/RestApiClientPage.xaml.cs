// Views/RestApiClientPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class RestApiClientPage : UserControl
{
    public RestApiClientPage(RestApiClientViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}