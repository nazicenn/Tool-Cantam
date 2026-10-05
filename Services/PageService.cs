using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions;

namespace tool_cantam.Services;

public class PageService : INavigationViewPageProvider
{
    private readonly IServiceProvider _serviceProvider;

    public PageService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public object? GetPage(Type pageType)
    {
        if (!typeof(FrameworkElement).IsAssignableFrom(pageType))
            return null;

        return _serviceProvider.GetService(pageType);
    }
}
