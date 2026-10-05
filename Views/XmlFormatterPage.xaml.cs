// Views/XmlFormatterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class XmlFormatterPage : UserControl
{
    public XmlFormatterPage(XmlFormatterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}