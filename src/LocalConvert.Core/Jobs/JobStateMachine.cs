namespace LocalConvert.Core.Jobs;

public static class JobStateMachine
{
    public static bool CanTransition(JobStatus from, JobStatus to)
    {
        return (from, to) switch
        {
            (JobStatus.Queued, JobStatus.Processing) => true,
            (JobStatus.Queued, JobStatus.Cancelled) => true,
            (JobStatus.Processing, JobStatus.Completed) => true,
            (JobStatus.Processing, JobStatus.Failed) => true,
            (JobStatus.Processing, JobStatus.Cancelled) => true,
            _ => false
        };
    }
}
