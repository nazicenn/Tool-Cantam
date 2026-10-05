using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class UrlPage : UserControl
{
    public UrlPage(UrlViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {

    }
}