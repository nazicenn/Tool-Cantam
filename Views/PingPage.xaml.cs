using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views
{
    public partial class PingPage : UserControl
    {
        public PingPage(PingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}