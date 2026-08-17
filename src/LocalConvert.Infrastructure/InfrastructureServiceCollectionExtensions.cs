using LocalConvert.Core.Files;
using LocalConvert.Core.Paths;
using LocalConvert.Core.Settings;
using LocalConvert.Infrastructure.Files;
using LocalConvert.Infrastructure.Paths;
using LocalConvert.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocalConvert.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLocalConvertInfrastructure(
        this IServiceCollection services,
        IAppPaths? appPaths = null)
    {
        var paths = appPaths ?? new WindowsAppPaths();
        if (paths is WindowsAppPaths windowsPaths)
        {
            windowsPaths.EnsureCreated();
        }
        else
        {
            Directory.CreateDirectory(paths.AppDataRoot);
            Directory.CreateDirectory(paths.TempRoot);
            Directory.CreateDirectory(paths.LogsRoot);
        }

        services.AddSingleton(paths);
        services.AddSingleton<IAppSettingsStore>(_ => new JsonAppSettingsStore(paths.SettingsFilePath));
        services.AddSingleton<ITemporaryFileService>(provider =>
            new TemporaryFileService(paths.TempRoot, provider.GetRequiredService<ILogger<TemporaryFileService>>()));

        return services;
    }
}
