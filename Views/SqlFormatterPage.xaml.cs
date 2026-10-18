using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class SqlFormatterPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.SqlFormatterViewModel>
{
    public ViewModels.SqlFormatterViewModel ViewModel { get; }
    public SqlFormatterPage(ViewModels.SqlFormatterViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}