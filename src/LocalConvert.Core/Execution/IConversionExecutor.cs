using LocalConvert.Core.Conversion;
using LocalConvert.Core.Jobs;

namespace LocalConvert.Core.Execution;

public interface IConversionExecutor
{
    Task<ConversionResult> ExecuteAsync(
        ConversionJob job,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken);
}
