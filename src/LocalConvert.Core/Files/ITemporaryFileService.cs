namespace LocalConvert.Core.Files;

public interface ITemporaryFileService
{
    string RootPath { get; }

    string CreateJobDirectory(Guid jobId);

    void DeleteJobDirectory(Guid jobId);

    void CleanupOrphanedDirectories();
}
