// Views/SlugGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class SlugGeneratorPage : UserControl
{
    public SlugGeneratorPage(SlugGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}