using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class HtmlEncoderPage : UserControl
{
    public HtmlEncoderPage(HtmlEncoderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}