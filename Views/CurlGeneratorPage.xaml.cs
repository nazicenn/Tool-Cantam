// Views/CurlGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class CurlGeneratorPage : UserControl
{
    public CurlGeneratorPage(CurlGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}