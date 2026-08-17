using System.Collections.Concurrent;
using System.Threading.Channels;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Execution;
using LocalConvert.Core.Jobs;
using Microsoft.Extensions.Logging;

namespace LocalConvert.Worker;

public sealed class ConversionJobQueue : IJobQueue, IDisposable
{
    private readonly IConversionExecutor _executor;
    private readonly ILogger<ConversionJobQueue> _logger;
    private readonly Channel<Guid> _channel;
    private readonly ConcurrentDictionary<Guid, ConversionJob> _jobs = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task[] _workers;
    private readonly int _workerCount;

    public ConversionJobQueue(
        IConversionExecutor executor,
        ILogger<ConversionJobQueue> logger,
        int? workerCount = null)
    {
        _executor = executor;
        _logger = logger;
        _workerCount = ResolveWorkerCount(workerCount);
        _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        _workers = Enumerable.Range(0, _workerCount)
            .Select(_ => Task.Run(() => ProcessLoopAsync(_shutdown.Token)))
            .ToArray();
    }

    public event EventHandler<ConversionJob>? JobUpdated;

    public static int ResolveWorkerCount(int? configured)
    {
        if (configured is > 0)
        {
            return Math.Min(configured.Value, 4);
        }

        var auto = Math.Max(1, Environment.ProcessorCount / 2);
        return Math.Min(auto, 2);
    }

    public ConversionJob Enqueue(ConversionJobRequest request)
    {
        var job = new ConversionJob
        {
            InputFiles = request.InputFiles,
            ConverterId = request.ConverterId,
            OutputDirectory = request.OutputDirectory,
            OutputFileName = request.OutputFileName,
            ExistingFilePolicy = request.ExistingFilePolicy,
            Options = request.Options
        };

        _jobs[job.Id] = job;
        if (!_channel.Writer.TryWrite(job.Id))
        {
            job.TryTransitionTo(JobStatus.Failed, ConversionError.From(ConversionErrorCode.Unknown));
        }

        RaiseUpdated(job);
        return job;
    }

    public bool TryCancel(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        job.Cancellation.Cancel();

        if (job.Status == JobStatus.Queued && job.TryTransitionTo(JobStatus.Cancelled))
        {
            RaiseUpdated(job);
            return true;
        }

        return true;
    }

    public ConversionJob? GetJob(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _channel.Writer.TryComplete();
        try
        {
            Task.WaitAll(_workers, TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // Shutdown is best-effort.
        }

        _shutdown.Dispose();
    }

    private async Task ProcessLoopAsync(CancellationToken shutdownToken)
    {
        await foreach (var jobId in _channel.Reader.ReadAllAsync(shutdownToken).ConfigureAwait(false))
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                continue;
            }

            if (job.Status == JobStatus.Cancelled)
            {
                continue;
            }

            if (!job.TryTransitionTo(JobStatus.Processing))
            {
                continue;
            }

            RaiseUpdated(job);

            try
            {
                var progress = new Progress<ConversionProgress>(update =>
                {
                    job.Progress = update;
                    RaiseUpdated(job);
                });

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    shutdownToken,
                    job.Cancellation.Token);

                var result = await _executor.ExecuteAsync(job, progress, linked.Token).ConfigureAwait(false);

                if (result.Success)
                {
                    job.OutputFiles = result.OutputPaths;
                    job.TryTransitionTo(JobStatus.Completed);
                }
                else if (result.Error?.Code == ConversionErrorCode.Cancelled ||
                         job.Cancellation.IsCancellationRequested)
                {
                    job.TryTransitionTo(JobStatus.Cancelled, result.Error);
                }
                else
                {
                    job.TryTransitionTo(JobStatus.Failed, result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                job.TryTransitionTo(JobStatus.Cancelled, ConversionError.From(ConversionErrorCode.Cancelled));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled worker error for job {JobId}.", job.Id);
                job.TryTransitionTo(JobStatus.Failed, ConversionError.From(ConversionErrorCode.Unknown, exception));
            }

            RaiseUpdated(job);
        }
    }

    private void RaiseUpdated(ConversionJob job)
    {
        JobUpdated?.Invoke(this, job);
    }
}
