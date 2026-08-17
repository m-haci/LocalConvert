using LocalConvert.Core.Conversion;
using LocalConvert.Core.Office;
using Microsoft.Extensions.DependencyInjection;

namespace LocalConvert.Office;

public static class OfficeServiceCollectionExtensions
{
    public static IServiceCollection AddLocalConvertOffice(this IServiceCollection services)
    {
        services.AddSingleton<IOfficeAvailability, WindowsOfficeDetector>();
        services.AddSingleton<IFileConverter, WordToPdfConverter>();
        services.AddSingleton<IFileConverter, PowerPointToPdfConverter>();
        services.AddSingleton<IFileConverter, ExcelToPdfConverter>();
        return services;
    }
}
