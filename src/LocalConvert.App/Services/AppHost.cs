using LocalConvert.App.Navigation;
using LocalConvert.App.ViewModels;
using LocalConvert.App.Views;
using LocalConvert.Infrastructure;
using LocalConvert.Infrastructure.Logging;
using LocalConvert.Infrastructure.Paths;
using LocalConvert.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LocalConvert.App.Services;

public static class AppHost
{
    public static IServiceProvider Build()
    {
        var paths = new WindowsAppPaths();
        paths.EnsureCreated();

        var services = new ServiceCollection();
        Log.Logger = SerilogSetup.CreateLogger(paths);
        services.AddLogging(builder => builder.AddSerilog(Log.Logger, dispose: true));
        services.AddLocalConvertInfrastructure(paths);
        services.AddLocalConvertWorker();
        services.AddSingleton<IAppNavigation, AppNavigation>();
        services.AddSingleton<IUiText, ResourceUiText>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<ConversionSessionState>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ConversionViewModel>();
        services.AddTransient<CompletedViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
