// Views/JsonMinifierPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class JsonMinifierPage : UserControl
{
    public JsonMinifierPage(JsonMinifierViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}