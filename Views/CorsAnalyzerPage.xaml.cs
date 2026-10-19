using Wpf.Ui.Abstractions.Controls;
namespace tool_cantam.Views;

public partial class CorsAnalyzerPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.CorsAnalyzerViewModel>
{
    public ViewModels.CorsAnalyzerViewModel ViewModel { get; }
    public CorsAnalyzerPage(ViewModels.CorsAnalyzerViewModel vm) { ViewModel = vm; DataContext = this; InitializeComponent(); }
}