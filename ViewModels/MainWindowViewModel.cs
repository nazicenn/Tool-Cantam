using CommunityToolkit.Mvvm.ComponentModel;

namespace tool_cantam.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Tool Cantam v1.01";
}