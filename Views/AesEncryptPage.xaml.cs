using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class AesEncryptPage : UserControl
{
    public AesEncryptPage(AesEncryptViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}