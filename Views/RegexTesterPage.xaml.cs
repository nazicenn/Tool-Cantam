// Views/RegexTesterPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class RegexTesterPage : UserControl
{
    public RegexTesterPage(RegexTesterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}