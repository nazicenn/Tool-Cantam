// Views/UuidGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class UuidGeneratorPage : UserControl
{
    public UuidGeneratorPage(UuidGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}