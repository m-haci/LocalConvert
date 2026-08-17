using FluentAssertions;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Execution;
using LocalConvert.Core.Files;
using LocalConvert.Core.Jobs;
using LocalConvert.Infrastructure.Files;
using LocalConvert.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class ConversionJobQueueTests
{
    [Fact]
    public async Task Enqueue_CompletesSuccessfulJob()
    {
        using var harness = new QueueHarness(delay: TimeSpan.Zero);
        var job = harness.Queue.Enqueue(CreateRequest());

        await WaitForStatus(job, JobStatus.Completed);

        job.Status.Should().Be(JobStatus.Completed);
        job.OutputFiles.Should().ContainSingle();
    }

    [Fact]
    public async Task TryCancel_StopsQueuedOrRunningJob()
    {
        using var harness = new QueueHarness(delay: TimeSpan.FromSeconds(3));
        var job = harness.Queue.Enqueue(CreateRequest());

        harness.Queue.TryCancel(job.Id).Should().BeTrue();
        await WaitForStatus(job, JobStatus.Cancelled, JobStatus.Failed);

        job.Status.Should().Be(JobStatus.Cancelled);
    }

    private static ConversionJobRequest CreateRequest()
    {
        return new ConversionJobRequest
        {
            InputFiles = ["a.jpg"],
            ConverterId = ConverterIds.ImageToPdf,
            OutputDirectory = Path.GetTempPath(),
            OutputFileName = "a.pdf"
        };
    }

    private static async Task WaitForStatus(ConversionJob job, params JobStatus[] expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            if (expected.Contains(job.Status))
            {
                return;
            }

            await Task.Delay(30);
        }

        throw new TimeoutException($"Job stayed in {job.Status}.");
    }

    private sealed class QueueHarness : IDisposable
    {
        private readonly string _tempRoot;

        public QueueHarness(TimeSpan delay)
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "LocalConvertQueue", Guid.NewGuid().ToString("N"));
            var catalog = new ConverterCatalog([new DelayedConverter(delay)]);
            var temp = new TemporaryFileService(_tempRoot, NullLogger<TemporaryFileService>.Instance);
            var executor = new InProcessConversionExecutor(
                catalog,
                temp,
                NullLogger<InProcessConversionExecutor>.Instance);
            Queue = new ConversionJobQueue(executor, NullLogger<ConversionJobQueue>.Instance, workerCount: 1);
        }

        public ConversionJobQueue Queue { get; }

        public void Dispose()
        {
            Queue.Dispose();
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
    }

    private sealed class DelayedConverter : IFileConverter
    {
        private readonly TimeSpan _delay;

        public DelayedConverter(TimeSpan delay)
        {
            _delay = delay;
        }

        public string Id => ConverterIds.ImageToPdf;

        public string DisplayNameKey => "Tool_ImageToPdf";

        public string DescriptionKey => "Tool_ImageToPdf_Desc";

        public string Glyph => "\uE91B";

        public ConverterCategory Category => ConverterCategory.Convert | ConverterCategory.Images;

        public IReadOnlyList<string> InputExtensions => FileExtensions.Images;

        public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

        public bool SupportsMultipleInputs => true;

        public bool CanConvert(ConversionInput input) => true;

        public async Task<ConversionResult> ConvertAsync(
            ConversionRequest request,
            IProgress<ConversionProgress>? progress,
            CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            Directory.CreateDirectory(request.OutputDirectory);
            var output = Path.Combine(request.OutputDirectory, request.OutputFileName);
            await File.WriteAllBytesAsync(output, [1, 2, 3], cancellationToken);
            return ConversionResult.Succeeded([output], TimeSpan.FromMilliseconds(1));
        }
    }
}
