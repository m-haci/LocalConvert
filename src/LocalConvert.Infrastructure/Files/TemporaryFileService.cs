using LocalConvert.Core.Files;
using Microsoft.Extensions.Logging;

namespace LocalConvert.Infrastructure.Files;

public sealed class TemporaryFileService : ITemporaryFileService
{
    private readonly ILogger<TemporaryFileService> _logger;

    public TemporaryFileService(string rootPath, ILogger<TemporaryFileService> logger)
    {
        RootPath = rootPath;
        _logger = logger;
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string CreateJobDirectory(Guid jobId)
    {
        var path = GetJobDirectory(jobId);
        Directory.CreateDirectory(path);
        return path;
    }

    public void DeleteJobDirectory(Guid jobId)
    {
        TryDeleteDirectory(GetJobDirectory(jobId));
    }

    public void CleanupOrphanedDirectories()
    {
        if (!Directory.Exists(RootPath))
        {
            return;
        }

        foreach (var directory in Directory.GetDirectories(RootPath))
        {
            TryDeleteDirectory(directory);
        }
    }

    private string GetJobDirectory(Guid jobId)
    {
        return Path.Combine(RootPath, jobId.ToString("N"));
    }

    private void TryDeleteDirectory(string directory)
    {
        if (!FilePathSafety.IsUnderRoot(directory, RootPath))
        {
            _logger.LogWarning("Skipped temp cleanup outside the temp root.");
            return;
        }

        if (!Directory.Exists(directory))
        {
            return;
        }

        if (FilePathSafety.IsReparsePoint(directory))
        {
            _logger.LogWarning("Skipped temp cleanup for a reparse point.");
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not delete a temporary directory.");
        }
    }
}
