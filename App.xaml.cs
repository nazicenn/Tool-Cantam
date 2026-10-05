using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;
using System.Windows.Media;
using tool_cantam.Core.Contracts;
using tool_cantam.Infrastructure;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace tool_cantam;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        SetupExceptionHandling();

        base.OnStartup(e);

        var sc = new ServiceCollection();
        ConfigureServices(sc);
        Services = sc.BuildServiceProvider();

        InitializeApplicationSettings();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void InitializeApplicationSettings()
    {
        if (Services == null) return;

        var settingsService = Services.GetRequiredService<ISettingsService>();
        var appSettings = settingsService.Load();

        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        ApplicationAccentColorManager.Apply(
            Color.FromRgb(0, 255, 65),
            ApplicationTheme.Dark
        );
    }

    private void ConfigureServices(IServiceCollection sc)
    {
        sc.AddLogging(builder => builder.AddSerilog());

        // ============================================
        // UI CORE SERVICES (manuel)
        // ============================================
        sc.AddSingleton<MainWindow>();
        sc.AddSingleton<MainWindowViewModel>();
        sc.AddSingleton<INavigationService, NavigationService>();
        sc.AddSingleton<Wpf.Ui.Abstractions.INavigationViewPageProvider, PageService>();
        sc.AddSingleton<ISnackbarService, SnackbarService>();
        sc.AddSingleton<IContentDialogService, ContentDialogService>();
        sc.AddSingleton<ISettingsService, SettingsService>();
        sc.AddSingleton<IFileExportService, FileExportService>();
        sc.AddSingleton<IToolLogger, ToolLogger>();
        sc.AddSingleton<IHttpHeaderService, HttpHeaderService>();

        // ============================================
        // OTOMATİK KAYITLAR (Service + ViewModel + Page)
        // ============================================
        sc.AddAllServices();

        // ============================================
        // TOOL TANIMLARI (manuel, statik ToolRegistry'e)
        // ============================================
        ToolDefinitions.RegisterAll();
    }

    private void SetupExceptionHandling()
    {
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Fatal(args.Exception, "UI Thread Crash");
            MessageBox.Show($"Unexpected error: {args.Exception.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Fatal(ex, "AppDomain Crash");
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}