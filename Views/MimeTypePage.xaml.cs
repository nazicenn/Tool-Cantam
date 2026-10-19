using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class MimeTypePage : System.Windows.Controls.UserControl, INavigableView<ViewModels.MimeTypeViewModel>
{
    public ViewModels.MimeTypeViewModel ViewModel { get; }
    public MimeTypePage(ViewModels.MimeTypeViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}