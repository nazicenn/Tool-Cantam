using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Infrastructure;

namespace tool_cantam.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        LoadAllTools();
    }

    public void LoadAllTools()
    {
        var tools = ToolRegistry.Tools;
        var grouped = tools
            .GroupBy(t => t.Category)
            .Select(g => new { Key = GetCategoryName(g.Key), Value = g.ToList() });
        CategoriesList.ItemsSource = grouped;
    }

    public void ShowSearchResults(List<ToolDefinition> results)
    {
        if (results.Count == 0)
        {
            CategoriesList.ItemsSource = null;
            return;
        }
        var grouped = results
            .GroupBy(t => t.Category)
            .Select(g => new { Key = GetCategoryName(g.Key), Value = g.ToList() });
        CategoriesList.ItemsSource = grouped;
    }

    private string GetCategoryName(ToolCategory category) => category switch
    {
        ToolCategory.EncodersDecoders => "Encoders & Decoders",
        ToolCategory.HashAndCrypto => "Hash & Crypto",
        ToolCategory.NetworkScanner => "Network & Scanner",
        ToolCategory.TextAndString => "Text & String",
        ToolCategory.FileAndMetadata => "File & Metadata",
        ToolCategory.Encryption => "Encryption",
        ToolCategory.DeveloperTools => "Developer Tools",
        ToolCategory.DateTime => "Date & Time",
        ToolCategory.ForensicOsint => "Forensic & OSINT",
        ToolCategory.PasswordCredential => "Password & Credential",
        ToolCategory.ApiWeb => "API & Web",
        ToolCategory.MathsConverters => "Maths & Converters",
        _ => "Other"
    };

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is ToolDefinition tool && tool.ViewType != null)
        {
            var page = App.Services!.GetRequiredService(tool.ViewType) as UserControl;
            if (page != null)
            {
                var mainWindow = App.Services!.GetRequiredService<MainWindow>();
                mainWindow.NavigateToPage(page);
            }
        }
    }
}