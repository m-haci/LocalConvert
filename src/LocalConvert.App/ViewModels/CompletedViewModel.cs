using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using Windows.System;

namespace LocalConvert.App.ViewModels;

public sealed partial class CompletedViewModel : ObservableObject
{
    private readonly ConversionSessionState _session;
    private readonly IAppNavigation _navigation;
    private readonly IUiText _text;

    public CompletedViewModel(ConversionSessionState session, IAppNavigation navigation, IUiText text)
    {
        _session = session;
        _navigation = navigation;
        _text = text;
        Refresh();
    }

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private string fileCountText = string.Empty;

    [ObservableProperty]
    private string fileSize = string.Empty;

    [ObservableProperty]
    private string durationText = string.Empty;

    public void Refresh()
    {
        var first = _session.OutputPaths.FirstOrDefault();
        FileName = first is null ? string.Empty : Path.GetFileName(first);
        FileCountText = _session.OutputPaths.Count <= 1
            ? string.Empty
            : string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                _text.Get("Completed_FileCount"),
                _session.OutputPaths.Count);
        FileSize = first is null || !File.Exists(first)
            ? string.Empty
            : FormatSize(new FileInfo(first).Length);
        DurationText = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            _text.Get("Completed_DurationFormat"),
            _session.Duration.TotalSeconds);
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var first = _session.OutputPaths.FirstOrDefault();
        if (first is null || !File.Exists(first))
        {
            return;
        }

        await Launcher.LaunchUriAsync(new Uri(first));
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var first = _session.OutputPaths.FirstOrDefault();
        if (first is null)
        {
            return;
        }

        var folder = Path.GetDirectoryName(first);
        if (folder is not null)
        {
            await Launcher.LaunchFolderPathAsync(folder);
        }
    }

    [RelayCommand]
    private void ConvertAnother()
    {
        _session.Reset();
        _navigation.GoHome();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.#} KB";
        }

        return $"{bytes / (1024d * 1024d):0.#} MB";
    }
}
