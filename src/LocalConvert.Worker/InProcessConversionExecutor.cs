using LocalConvert.Core.Conversion;
using LocalConvert.Core.Execution;
using LocalConvert.Core.Files;
using LocalConvert.Core.Jobs;
using LocalConvert.Core.Logging;
using Microsoft.Extensions.Logging;

namespace LocalConvert.Worker;

public sealed class InProcessConversionExecutor : IConversionExecutor
{
    private readonly IConverterCatalog _catalog;
    private readonly ITemporaryFileService _temporaryFileService;
    private readonly ILogger<InProcessConversionExecutor> _logger;

    public InProcessConversionExecutor(
        IConverterCatalog catalog,
        ITemporaryFileService temporaryFileService,
        ILogger<InProcessConversionExecutor> logger)
    {
        _catalog = catalog;
        _temporaryFileService = temporaryFileService;
        _logger = logger;
    }

    public async Task<ConversionResult> ExecuteAsync(
        ConversionJob job,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken)
    {
        var converter = _catalog.GetById(job.ConverterId);
        if (converter is null)
        {
            return ConversionResult.Failed(ConversionErrorCode.ConverterNotFound);
        }

        string? jobTempDirectory = null;

        try
        {
            jobTempDirectory = _temporaryFileService.CreateJobDirectory(job.Id);

            var request = new ConversionRequest
            {
                JobId = job.Id,
                InputPaths = job.InputFiles,
                OutputDirectory = jobTempDirectory,
                OutputFileName = job.OutputFileName,
                ConverterId = job.ConverterId,
                Options = job.Options
            };

            var result = await converter.ConvertAsync(request, progress, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                return result;
            }

            Directory.CreateDirectory(job.OutputDirectory);
            var moved = new List<string>(result.OutputPaths.Count);

            foreach (var temporaryPath in result.OutputPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationName = Path.GetFileName(temporaryPath);
                var resolution = OutputFileNameGenerator.Resolve(
                    job.OutputDirectory,
                    destinationName,
                    job.ExistingFilePolicy);
                if (resolution.RequiresUserConfirmation)
                {
                    resolution = OutputFileNameGenerator.Resolve(
                        job.OutputDirectory,
                        destinationName,
                        ExistingFilePolicy.CreateNewName);
                }

                File.Move(temporaryPath, resolution.FullPath, overwrite: job.ExistingFilePolicy == ExistingFilePolicy.Overwrite);
                moved.Add(resolution.FullPath);
            }

            _logger.LogInformation(
                "Job {JobId} produced {Count} file(s), first {FileName}.",
                job.Id,
                moved.Count,
                LogSanitizer.FileNameOnly(moved.FirstOrDefault()));

            return ConversionResult.Succeeded(moved, result.Duration);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Job {JobId} failed during execution.", job.Id);
            return ConversionResult.Failed(ConversionErrorCode.OutputWriteFailed, exception);
        }
        finally
        {
            if (jobTempDirectory is not null)
            {
                _temporaryFileService.DeleteJobDirectory(job.Id);
            }
        }
    }
}
