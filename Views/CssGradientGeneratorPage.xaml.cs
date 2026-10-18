using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class CssGradientGeneratorPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CssGradientGeneratorViewModel>
{
    public ViewModels.CssGradientGeneratorViewModel ViewModel { get; }
    public CssGradientGeneratorPage(ViewModels.CssGradientGeneratorViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}