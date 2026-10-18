using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

public partial class ColorPickerPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.ColorPickerViewModel>
{
    public ViewModels.ColorPickerViewModel ViewModel { get; }

    public ColorPickerPage(ViewModels.ColorPickerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}