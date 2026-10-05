using System.Windows.Controls;
using Wpf.Ui.Abstractions.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class SettingsPage : UserControl, INavigableView<SettingsViewModel>
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}