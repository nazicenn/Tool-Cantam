using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class JsonFormatterPage : UserControl
{
    public JsonFormatterPage(JsonFormatterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}