using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class UnixTimestampPage : UserControl
{
    public UnixTimestampPage(UnixTimestampViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}