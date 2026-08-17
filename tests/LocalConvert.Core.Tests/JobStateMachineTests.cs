using FluentAssertions;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Jobs;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class JobStateMachineTests
{
    [Theory]
    [InlineData(JobStatus.Queued, JobStatus.Processing, true)]
    [InlineData(JobStatus.Queued, JobStatus.Cancelled, true)]
    [InlineData(JobStatus.Processing, JobStatus.Completed, true)]
    [InlineData(JobStatus.Processing, JobStatus.Failed, true)]
    [InlineData(JobStatus.Processing, JobStatus.Cancelled, true)]
    [InlineData(JobStatus.Queued, JobStatus.Completed, false)]
    [InlineData(JobStatus.Completed, JobStatus.Processing, false)]
    [InlineData(JobStatus.Failed, JobStatus.Queued, false)]
    [InlineData(JobStatus.Cancelled, JobStatus.Processing, false)]
    public void CanTransition_EnforcesLegalPaths(JobStatus from, JobStatus to, bool expected)
    {
        JobStateMachine.CanTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public void ConversionJob_RecordsTimestampsOnTransition()
    {
        var job = new ConversionJob
        {
            InputFiles = ["a.jpg"],
            ConverterId = ConverterIds.ImageToPdf,
            OutputDirectory = @"C:\out",
            OutputFileName = "a.pdf"
        };

        job.Status.Should().Be(JobStatus.Queued);
        job.TryTransitionTo(JobStatus.Processing).Should().BeTrue();
        job.StartedAt.Should().NotBeNull();
        job.TryTransitionTo(JobStatus.Completed).Should().BeTrue();
        job.CompletedAt.Should().NotBeNull();
        job.TryTransitionTo(JobStatus.Failed).Should().BeFalse();
    }
}
