// Views/CronParserPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class CronParserPage : UserControl
{
    public CronParserPage(CronParserViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}