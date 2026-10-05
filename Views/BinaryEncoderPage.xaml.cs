using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class BinaryEncoderPage : UserControl
{
    public BinaryEncoderPage(BinaryEncoderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}