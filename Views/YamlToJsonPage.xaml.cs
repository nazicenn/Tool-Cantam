// Views/YamlToJsonPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class YamlToJsonPage : UserControl
{
    public YamlToJsonPage(YamlToJsonViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}