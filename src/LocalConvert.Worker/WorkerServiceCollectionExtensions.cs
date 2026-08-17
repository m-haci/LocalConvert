using LocalConvert.Core.Conversion;
using LocalConvert.Core.Execution;
using LocalConvert.Core.Jobs;
using LocalConvert.Core.Pdf;
using LocalConvert.Core.Suggestions;
using LocalConvert.Images;
using LocalConvert.Office;
using LocalConvert.Pdf;
using Microsoft.Extensions.DependencyInjection;

namespace LocalConvert.Worker;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddLocalConvertWorker(this IServiceCollection services)
    {
        services.AddLocalConvertOffice();
        services.AddSingleton<IFileConverter, ImageToPdfConverter>();
        services.AddSingleton<IFileConverter, PdfMergeConverter>();
        services.AddSingleton<IFileConverter, PdfSplitConverter>();
        services.AddSingleton<IFileConverter, PdfExtractConverter>();
        services.AddSingleton<IFileConverter, PdfRotateConverter>();
        services.AddSingleton<IFileConverter, PdfReorderConverter>();
        services.AddSingleton<IFileConverter, PdfMetadataConverter>();
        services.AddSingleton<IFileConverter, PdfToJpegConverter>();
        services.AddSingleton<IFileConverter, PdfToPngConverter>();
        services.AddSingleton<IPdfMetadataReader, PdfPigMetadataReader>();
        services.AddSingleton<IConverterCatalog, ConverterCatalog>();
        services.AddSingleton<OperationSuggester>();
        services.AddSingleton<IConversionExecutor, InProcessConversionExecutor>();
        services.AddSingleton<IJobQueue, ConversionJobQueue>();
        return services;
    }
}
