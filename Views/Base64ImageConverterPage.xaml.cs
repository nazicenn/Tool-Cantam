// Views/Base64ImageConverterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class Base64ImageConverterPage : UserControl
{
    public Base64ImageConverterPage(Base64ImageConverterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}