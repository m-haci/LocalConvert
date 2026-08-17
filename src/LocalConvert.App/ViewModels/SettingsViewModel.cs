using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalConvert.App.Services;
using LocalConvert.Core.Files;
using LocalConvert.Core.Office;
using LocalConvert.Core.Settings;
using Windows.Globalization;

namespace LocalConvert.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsStore _store;
    private readonly IFileDialogService _fileDialog;

    public SettingsViewModel(IAppSettingsStore store, IFileDialogService fileDialog)
    {
        _store = store;
        _fileDialog = fileDialog;
        var settings = store.Current;
        UseSameFolder = settings.OutputFolderMode == OutputFolderMode.SameAsSource;
        UseCustomFolder = !UseSameFolder;
        CustomOutputFolder = settings.CustomOutputFolder ?? string.Empty;
        CreateNewName = settings.ExistingFilePolicy == ExistingFilePolicy.CreateNewName;
        AskWhenExists = settings.ExistingFilePolicy == ExistingFilePolicy.Ask;
        OverwriteExisting = settings.ExistingFilePolicy == ExistingFilePolicy.Overwrite;
        UseTurkish = settings.Language.StartsWith("tr", StringComparison.OrdinalIgnoreCase);
        UseEnglish = !UseTurkish;
        UseOfficeAuto = settings.OfficeEnginePreference == OfficeEnginePreference.Auto;
        UseOfficeMicrosoft = settings.OfficeEnginePreference == OfficeEnginePreference.MicrosoftOffice;
        UseOfficeLibre = settings.OfficeEnginePreference == OfficeEnginePreference.LibreOffice;
    }

    [ObservableProperty]
    private bool useSameFolder;

    [ObservableProperty]
    private bool useCustomFolder;

    [ObservableProperty]
    private string customOutputFolder = string.Empty;

    [ObservableProperty]
    private bool createNewName;

    [ObservableProperty]
    private bool askWhenExists;

    [ObservableProperty]
    private bool overwriteExisting;

    [ObservableProperty]
    private bool useTurkish;

    [ObservableProperty]
    private bool useEnglish;

    [ObservableProperty]
    private bool useOfficeAuto;

    [ObservableProperty]
    private bool useOfficeMicrosoft;

    [ObservableProperty]
    private bool useOfficeLibre;

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        var folder = await _fileDialog.PickFolderAsync();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            CustomOutputFolder = folder;
            UseCustomFolder = true;
            UseSameFolder = false;
            await SaveAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = new AppSettings
        {
            OutputFolderMode = UseCustomFolder ? OutputFolderMode.CustomFolder : OutputFolderMode.SameAsSource,
            CustomOutputFolder = string.IsNullOrWhiteSpace(CustomOutputFolder) ? null : CustomOutputFolder,
            ExistingFilePolicy = OverwriteExisting
                ? ExistingFilePolicy.Overwrite
                : AskWhenExists
                    ? ExistingFilePolicy.Ask
                    : ExistingFilePolicy.CreateNewName,
            Language = UseEnglish ? "en-US" : "tr-TR",
            OfficeEnginePreference = UseOfficeMicrosoft
                ? OfficeEnginePreference.MicrosoftOffice
                : UseOfficeLibre
                    ? OfficeEnginePreference.LibreOffice
                    : OfficeEnginePreference.Auto
        };

        await _store.SaveAsync(settings);
        try
        {
            ApplicationLanguages.PrimaryLanguageOverride = settings.Language;
        }
        catch (InvalidOperationException)
        {
            // Unpackaged WinUI apps can reject a runtime language override.
        }
    }
}
