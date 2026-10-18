using Wpf.Ui.Abstractions.Controls;

namespace tool_cantam.Views;

/// <summary>
/// Metadata temizleme araç sayfası. Drag-drop <see cref="Helpers.FileDragDropBehavior"/> ile sağlanır.
/// </summary>
public partial class MetadataPage : System.Windows.Controls.UserControl, INavigableView<ViewModels.MetadataViewModel>
{
    public ViewModels.MetadataViewModel ViewModel { get; }

    public MetadataPage(ViewModels.MetadataViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}