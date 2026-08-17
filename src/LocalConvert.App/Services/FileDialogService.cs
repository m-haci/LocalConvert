using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace LocalConvert.App.Services;

public sealed class FileDialogService : IFileDialogService
{
    public async Task<IReadOnlyList<string>> PickFilesAsync(IReadOnlyList<string> extensions)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List
        };

        foreach (var extension in extensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        Initialize(picker);
        var files = await picker.PickMultipleFilesAsync();
        return files is null ? [] : files.Select(file => file.Path).ToList();
    }

    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");
        Initialize(picker);
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private static void Initialize(object picker)
    {
        var hwnd = WindowNative.GetWindowHandle(App.MainAppWindow);
        InitializeWithWindow.Initialize(picker, hwnd);
    }
}
