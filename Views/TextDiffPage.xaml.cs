// Views/TextDiffPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class TextDiffPage : UserControl
{
    public TextDiffPage(TextDiffViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}