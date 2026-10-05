using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace tool_cantam.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddAllServices(this IServiceCollection sc)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Tüm *Service sınıflarını ve interface'lerini bul
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service") && t.Namespace != null && t.Namespace.Contains("Services"));

        foreach (var impl in serviceTypes)
        {
            var intf = impl.GetInterfaces().FirstOrDefault(i => i.Name.EndsWith("Service"));
            if (intf != null)
                sc.AddSingleton(intf, impl);
            else
                sc.AddSingleton(impl);
        }

        // Tüm ViewModel'leri bul
        var viewModelTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null && t.Namespace.Contains("ViewModels"));

        foreach (var vm in viewModelTypes)
        {
            sc.AddTransient(vm);
        }

        // Tüm Page/View'leri bul
        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null && t.Namespace.Contains("Views") && t.Name.EndsWith("Page"));

        foreach (var page in pageTypes)
        {
            sc.AddTransient(page);
        }

        
    }
}