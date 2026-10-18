using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class CurlGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CurlGeneratorViewModel>
{
    public ViewModels.CurlGeneratorViewModel ViewModel { get; }
    public CurlGeneratorPage(ViewModels.CurlGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}