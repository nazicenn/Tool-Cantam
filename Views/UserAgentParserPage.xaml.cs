// Views/UserAgentParserPage.xaml.cs
using System.Windows.Controls;
using tool_cantam.ViewModels;

namespace tool_cantam.Views;

public partial class UserAgentParserPage : UserControl
{
    public UserAgentParserPage(UserAgentParserViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SampleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            var userAgent = item.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                var viewModel = DataContext as UserAgentParserViewModel;
                if (viewModel != null)
                {
                    viewModel.UserAgent = userAgent;
                    viewModel.Parse();
                }
            }
        }
    }
}