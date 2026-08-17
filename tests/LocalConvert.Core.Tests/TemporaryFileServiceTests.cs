using FluentAssertions;
using LocalConvert.Infrastructure.Files;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class TemporaryFileServiceTests
{
    [Fact]
    public void CreateAndDeleteJobDirectory_RemovesOnlyThatJob()
    {
        var root = Path.Combine(Path.GetTempPath(), "LocalConvertTests", Guid.NewGuid().ToString("N"));
        var service = new TemporaryFileService(root, NullLogger<TemporaryFileService>.Instance);
        var jobId = Guid.NewGuid();

        var directory = service.CreateJobDirectory(jobId);
        File.WriteAllText(Path.Combine(directory, "scratch.txt"), "temp");
        Directory.Exists(directory).Should().BeTrue();

        service.DeleteJobDirectory(jobId);
        Directory.Exists(directory).Should().BeFalse();
        Directory.Exists(root).Should().BeTrue();
    }

    [Fact]
    public void CleanupOrphanedDirectories_RemovesLeftoverJobFolders()
    {
        var root = Path.Combine(Path.GetTempPath(), "LocalConvertTests", Guid.NewGuid().ToString("N"));
        var service = new TemporaryFileService(root, NullLogger<TemporaryFileService>.Instance);
        var orphan = service.CreateJobDirectory(Guid.NewGuid());
        File.WriteAllText(Path.Combine(orphan, "leftover.bin"), "x");

        service.CleanupOrphanedDirectories();

        Directory.GetDirectories(root).Should().BeEmpty();
    }

    [Fact]
    public void DeleteJobDirectory_DoesNotDeleteOutsideTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "LocalConvertTests", Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(Path.GetTempPath(), "LocalConvertTests-outside", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "keep.txt"), "keep");

        var service = new TemporaryFileService(root, NullLogger<TemporaryFileService>.Instance);
        service.DeleteJobDirectory(Guid.NewGuid());

        File.Exists(Path.Combine(outside, "keep.txt")).Should().BeTrue();
        Directory.Delete(outside, recursive: true);
    }
}
