// Views/ColorPickerPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class ColorPickerPage : UserControl
{
    public ColorPickerPage(ColorPickerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}