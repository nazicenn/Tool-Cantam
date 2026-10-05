// Views/RandomStringGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class RandomStringGeneratorPage : UserControl
{
    public RandomStringGeneratorPage(RandomStringGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}