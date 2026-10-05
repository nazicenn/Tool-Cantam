// Views/CsvToJsonPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class CsvToJsonPage : UserControl
{
    public CsvToJsonPage(CsvToJsonViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}