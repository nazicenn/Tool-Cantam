// Views/HttpRequestTesterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class HttpRequestTesterPage : UserControl
{
    public HttpRequestTesterPage(HttpRequestTesterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}