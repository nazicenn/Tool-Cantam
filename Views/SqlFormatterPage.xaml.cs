// Views/SqlFormatterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class SqlFormatterPage : UserControl
{
    public SqlFormatterPage(SqlFormatterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}