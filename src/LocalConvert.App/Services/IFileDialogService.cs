namespace LocalConvert.App.Services;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions);

    Task<string?> PickFolderAsync();
}
