using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using tool_cantam.Core.Enums;
using tool_cantam.Infrastructure;
using tool_cantam.ViewModels;
using tool_cantam.Views;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace tool_cantam;

public partial class MainWindow : FluentWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        INavigationViewPageProvider pageProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        WindowBackdropType = WindowBackdropType.None;

        InitializeComponent();

        var pureBlack = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        var textFg = new SolidColorBrush(Color.FromRgb(224, 224, 224));

        Background = pureBlack;
        SideNav.Background = pureBlack;
        SideNav.Foreground = textFg;

        navigationService.SetNavigationControl(SideNav);
        SideNav.SetPageProviderService(pageProvider);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialogPresenter);

        Loaded += (_, _) =>
        {
            BuildMenu();
            NavigateToPage(new DashboardPage());
        };
    }

    public void NavigateToPage(UserControl page)
    {
        MainFrame.Content = page;
    }

    private void NavigateToCategory(ToolCategory category)
    {
        var tools = ToolRegistry.Tools.Where(t => t.Category == category).ToList();

        var categoryPage = new CategoryCardsPage();
        categoryPage.CategoryName = GetCategoryName(category);
        categoryPage.Tools = tools;

        NavigateToPage(categoryPage);
    }

    private void SearchBox_TextChanged(object sender, RoutedEventArgs e)
    {
        var searchText = SearchBox.Text?.Trim() ?? "";
        var dashPage = App.Services!.GetRequiredService<DashboardPage>();

        if (string.IsNullOrWhiteSpace(searchText))
            dashPage.LoadAllTools();
        else
        {
            var results = ToolRegistry.Tools
                .Where(t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            t.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            dashPage.ShowSearchResults(results);
        }

        NavigateToPage(dashPage);
    }

    private void BuildMenu()
    {
        SideNav.MenuItems.Clear();
        SideNav.FooterMenuItems.Clear();

        var blackBg = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        var textFg = new SolidColorBrush(Color.FromRgb(224, 224, 224));
        var hoverBg = new SolidColorBrush(Color.FromRgb(10, 10, 10));

        var homeItem = new NavigationViewItem
        {
            Content = "Home",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
            Background = blackBg,
            Foreground = textFg
        };
        homeItem.PreviewMouseLeftButtonDown += (s, e) => NavigateToPage(new DashboardPage());
        SideNav.MenuItems.Add(homeItem);

        foreach (var group in ToolRegistry.Tools.GroupBy(t => t.Category))
        {
            var category = group.Key;
            var catItem = new NavigationViewItem
            {
                Content = GetCategoryName(category),
                Icon = new SymbolIcon { Symbol = GetCategoryIcon(category) },
                Background = blackBg,
                Foreground = textFg
            };
            catItem.PreviewMouseLeftButtonDown += (s, e) => NavigateToCategory(category);
            SideNav.MenuItems.Add(catItem);
        }

        var settingsItem = new NavigationViewItem
        {
            Content = "Settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            Background = blackBg,
            Foreground = textFg
        };
        settingsItem.PreviewMouseLeftButtonDown += (s, e) =>
        {
            var settingsPage = App.Services!.GetRequiredService<SettingsPage>();
            NavigateToPage(settingsPage);
        };
        SideNav.FooterMenuItems.Add(settingsItem);
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

    private SymbolRegular GetCategoryIcon(ToolCategory category) => category switch
    {
        ToolCategory.EncodersDecoders => SymbolRegular.Code24,
        ToolCategory.HashAndCrypto => SymbolRegular.Fingerprint24,
        ToolCategory.NetworkScanner => SymbolRegular.WifiSettings20,
        ToolCategory.TextAndString => SymbolRegular.Edit24,
        ToolCategory.FileAndMetadata => SymbolRegular.Document24,
        ToolCategory.Encryption => SymbolRegular.Shield24,
        ToolCategory.DeveloperTools => SymbolRegular.DeveloperBoard24,
        ToolCategory.DateTime => SymbolRegular.Calendar24,
        ToolCategory.ForensicOsint => SymbolRegular.Search24,
        ToolCategory.PasswordCredential => SymbolRegular.Key24,
        ToolCategory.ApiWeb => SymbolRegular.Cloud24,
        ToolCategory.MathsConverters => SymbolRegular.Calculator24,
        _ => SymbolRegular.AppGeneric24
    };
}