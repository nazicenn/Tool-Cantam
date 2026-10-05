// Views/RsaKeyGeneratorPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class RsaKeyGeneratorPage : UserControl
{
    public RsaKeyGeneratorPage(RsaKeyGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}