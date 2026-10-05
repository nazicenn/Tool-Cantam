using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using tool_cantam.Core.Models;
using tool_cantam.Infrastructure;

namespace tool_cantam.Views;

public partial class CategoryCardsPage : UserControl
{
    public static readonly DependencyProperty CategoryNameProperty =
        DependencyProperty.Register(nameof(CategoryName), typeof(string), typeof(CategoryCardsPage), new PropertyMetadata("TOOL CATEGORY"));

    public string CategoryName
    {
        get => (string)GetValue(CategoryNameProperty);
        set => SetValue(CategoryNameProperty, value);
    }

    public static readonly DependencyProperty ToolsProperty =
        DependencyProperty.Register(nameof(Tools), typeof(List<ToolDefinition>), typeof(CategoryCardsPage), new PropertyMetadata(null, OnToolsChanged));

    public List<ToolDefinition> Tools
    {
        get => (List<ToolDefinition>)GetValue(ToolsProperty);
        set => SetValue(ToolsProperty, value);
    }

    public CategoryCardsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private static void OnToolsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CategoryCardsPage page && e.NewValue is List<ToolDefinition> tools)
        {
            page.ToolsList.ItemsSource = tools;
        }
    }

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