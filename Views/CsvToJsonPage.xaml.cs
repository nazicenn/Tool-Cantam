using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class CsvToJsonPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CsvToJsonViewModel>
{
    public ViewModels.CsvToJsonViewModel ViewModel { get; }
    public CsvToJsonPage(ViewModels.CsvToJsonViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}