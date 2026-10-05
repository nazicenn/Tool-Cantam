using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class HttpHeaderPage : UserControl
{
    public HttpHeaderPage(HttpHeaderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}