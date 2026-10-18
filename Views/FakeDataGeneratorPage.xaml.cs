using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class FakeDataGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.FakeDataGeneratorViewModel>
{
    public ViewModels.FakeDataGeneratorViewModel ViewModel { get; }
    public FakeDataGeneratorPage(ViewModels.FakeDataGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}