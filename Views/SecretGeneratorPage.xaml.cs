// Views/SecretGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class SecretGeneratorPage : UserControl
{
    public SecretGeneratorPage(SecretGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}