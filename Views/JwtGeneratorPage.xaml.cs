// Views/JwtGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class JwtGeneratorPage : UserControl
{
    public JwtGeneratorPage(JwtGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}