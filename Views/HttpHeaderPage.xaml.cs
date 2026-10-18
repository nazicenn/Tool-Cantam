using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class HttpHeaderPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.HttpHeaderViewModel>
{
    public ViewModels.HttpHeaderViewModel ViewModel { get; }

    public HttpHeaderPage(ViewModels.HttpHeaderViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}