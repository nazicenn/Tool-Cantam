// Views/FakeDataGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class FakeDataGeneratorPage : UserControl
{
    public FakeDataGeneratorPage(FakeDataGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}