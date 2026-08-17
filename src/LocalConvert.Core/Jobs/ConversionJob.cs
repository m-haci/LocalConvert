using LocalConvert.Core.Conversion;
using LocalConvert.Core.Files;

namespace LocalConvert.Core.Jobs;

public sealed class ConversionJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required IReadOnlyList<string> InputFiles { get; init; }

    public required string ConverterId { get; init; }

    public required string OutputDirectory { get; init; }

    public required string OutputFileName { get; init; }

    public ExistingFilePolicy ExistingFilePolicy { get; init; } = ExistingFilePolicy.CreateNewName;

    public IReadOnlyDictionary<string, string> Options { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> OutputFiles { get; set; } = [];

    public JobStatus Status { get; private set; } = JobStatus.Queued;

    public ConversionProgress Progress { get; set; } = new();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public ConversionError? Error { get; private set; }

    public CancellationTokenSource Cancellation { get; } = new();

    public bool TryTransitionTo(JobStatus nextStatus, ConversionError? error = null)
    {
        if (!JobStateMachine.CanTransition(Status, nextStatus))
        {
            return false;
        }

        Status = nextStatus;

        if (nextStatus == JobStatus.Processing)
        {
            StartedAt = DateTimeOffset.Now;
        }

        if (nextStatus is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled)
        {
            CompletedAt = DateTimeOffset.Now;
            Error = error;
        }

        return true;
    }
}
