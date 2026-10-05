// Views/YamlFormatterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class YamlFormatterPage : UserControl
{
    public YamlFormatterPage(YamlFormatterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}