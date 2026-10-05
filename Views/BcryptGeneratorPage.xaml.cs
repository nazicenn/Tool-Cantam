// Views/BcryptGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class BcryptGeneratorPage : UserControl
{
    public BcryptGeneratorPage(BcryptGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}