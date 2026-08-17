using LocalConvert.Core.Conversion;

namespace LocalConvert.Core.Jobs;

public interface IJobQueue
{
    event EventHandler<ConversionJob>? JobUpdated;

    ConversionJob Enqueue(ConversionJobRequest request);

    bool TryCancel(Guid jobId);

    ConversionJob? GetJob(Guid jobId);
}
