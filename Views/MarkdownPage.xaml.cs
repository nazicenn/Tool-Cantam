// Views/MarkdownPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class MarkdownPage : UserControl
{
    private MarkdownViewModel _viewModel;

    public MarkdownPage(MarkdownViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.OutputHtml))
            {
                PreviewBrowser.NavigateToString(_viewModel.OutputHtml);
            }
        };
    }
}