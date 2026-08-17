using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Files;
using LocalConvert.Core.Jobs;
using LocalConvert.Core.Office;
using LocalConvert.Core.Pdf;
using LocalConvert.Core.Settings;
using Microsoft.UI.Xaml.Controls;

namespace LocalConvert.App.ViewModels;

public sealed partial class ConversionViewModel : ObservableObject
{
    private readonly ConversionSessionState _session;
    private readonly IJobQueue _jobQueue;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IFileDialogService _fileDialog;
    private readonly IAppNavigation _navigation;
    private readonly IUiText _text;
    private readonly IPdfMetadataReader _metadataReader;
    private readonly IOfficeAvailability _officeAvailability;
    private ConversionJob? _activeJob;

    public ConversionViewModel(
        ConversionSessionState session,
        IJobQueue jobQueue,
        IAppSettingsStore settingsStore,
        IFileDialogService fileDialog,
        IAppNavigation navigation,
        IUiText text,
        IPdfMetadataReader metadataReader,
        IOfficeAvailability officeAvailability)
    {
        _session = session;
        _jobQueue = jobQueue;
        _settingsStore = settingsStore;
        _fileDialog = fileDialog;
        _navigation = navigation;
        _text = text;
        _metadataReader = metadataReader;
        _officeAvailability = officeAvailability;
        _jobQueue.JobUpdated += OnJobUpdated;
        RefreshFromSession();
    }

    public ObservableCollection<InputFileItemViewModel> InputFiles { get; } = [];

    [ObservableProperty]
    private string converterTitle = string.Empty;

    [ObservableProperty]
    private string converterDescription = string.Empty;

    [ObservableProperty]
    private string converterGlyph = "\uE8A5";

    [ObservableProperty]
    private string convertButtonText = string.Empty;

    [ObservableProperty]
    private string outputFolder = string.Empty;

    [ObservableProperty]
    private string totalSizeText = string.Empty;

    [ObservableProperty]
    private string fileCountText = string.Empty;

    [ObservableProperty]
    private bool hasFiles;

    [ObservableProperty]
    private bool hasNoFiles;

    [ObservableProperty]
    private bool isMergeTool;

    [ObservableProperty]
    private bool isSplitTool;

    [ObservableProperty]
    private bool isExtractTool;

    [ObservableProperty]
    private bool isRotateTool;

    [ObservableProperty]
    private bool isReorderTool;

    [ObservableProperty]
    private bool isMetadataTool;

    [ObservableProperty]
    private bool isOfficeTool;

    [ObservableProperty]
    private bool showOfficeMissingHint;

    [ObservableProperty]
    private bool needMoreFiles;

    [ObservableProperty]
    private bool showNeedMoreFiles;

    [ObservableProperty]
    private string needMoreFilesText = string.Empty;

    [ObservableProperty]
    private bool splitAllPages = true;

    [ObservableProperty]
    private bool splitByRange;

    [ObservableProperty]
    private string pageRangeText = "1-5";

    [ObservableProperty]
    private string pageOrderText = "3,1,2,4";

    [ObservableProperty]
    private bool rotate90 = true;

    [ObservableProperty]
    private bool rotate180;

    [ObservableProperty]
    private bool rotate270;

    [ObservableProperty]
    private bool officeEngineAuto = true;

    [ObservableProperty]
    private bool officeEngineMicrosoft;

    [ObservableProperty]
    private bool officeEngineLibre;

    [ObservableProperty]
    private string metadataTitle = string.Empty;

    [ObservableProperty]
    private string metadataAuthor = string.Empty;

    [ObservableProperty]
    private string metadataSubject = string.Empty;

    [ObservableProperty]
    private string metadataCreator = string.Empty;

    [ObservableProperty]
    private string metadataProducer = string.Empty;

    [ObservableProperty]
    private string officeStatusText = string.Empty;

    [ObservableProperty]
    private bool hasOfficeWarning;

    [ObservableProperty]
    private bool isProcessing;

    [ObservableProperty]
    private int progressPercent;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private string? errorTitle;

    [ObservableProperty]
    private string? errorHint;

    [ObservableProperty]
    private string? errorDetail;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool canConvert;

    partial void OnSplitAllPagesChanged(bool value)
    {
        if (value)
        {
            SplitByRange = false;
        }
    }

    partial void OnSplitByRangeChanged(bool value)
    {
        if (value)
        {
            SplitAllPages = false;
        }
    }

    partial void OnRotate90Changed(bool value)
    {
        if (value)
        {
            Rotate180 = false;
            Rotate270 = false;
        }
    }

    partial void OnRotate180Changed(bool value)
    {
        if (value)
        {
            Rotate90 = false;
            Rotate270 = false;
        }
    }

    partial void OnRotate270Changed(bool value)
    {
        if (value)
        {
            Rotate90 = false;
            Rotate180 = false;
        }
    }

    partial void OnOfficeEngineAutoChanged(bool value)
    {
        if (value)
        {
            OfficeEngineMicrosoft = false;
            OfficeEngineLibre = false;
        }
    }

    partial void OnOfficeEngineMicrosoftChanged(bool value)
    {
        if (value)
        {
            OfficeEngineAuto = false;
            OfficeEngineLibre = false;
        }
    }

    partial void OnOfficeEngineLibreChanged(bool value)
    {
        if (value)
        {
            OfficeEngineAuto = false;
            OfficeEngineMicrosoft = false;
        }
    }

    public void Detach()
    {
        _jobQueue.JobUpdated -= OnJobUpdated;
    }

    public void RefreshFromSession()
    {
        RebuildFileList();
        ConverterTitle = _session.Converter is null
            ? string.Empty
            : _text.Get(_session.Converter.DisplayNameKey);
        ConverterDescription = _session.Converter is null
            ? string.Empty
            : _text.Get(_session.Converter.DescriptionKey);
        ConverterGlyph = _session.Converter?.Glyph ?? "\uE8A5";

        var converterId = _session.Converter?.Id;
        ConvertButtonText = converterId == ConverterIds.PdfMetadata
            ? _text.Get("Conversion_ClearMetadata")
            : _text.Get("Conversion_Convert");
        IsMergeTool = converterId == ConverterIds.PdfMerge;
        IsSplitTool = converterId == ConverterIds.PdfSplit;
        IsExtractTool = converterId == ConverterIds.PdfExtract;
        IsRotateTool = converterId == ConverterIds.PdfRotate;
        IsReorderTool = converterId == ConverterIds.PdfReorder;
        IsMetadataTool = converterId == ConverterIds.PdfMetadata;
        IsOfficeTool = converterId is ConverterIds.WordToPdf or ConverterIds.PowerPointToPdf or ConverterIds.ExcelToPdf;
        var availability = _officeAvailability.Detect();
        ShowOfficeMissingHint = IsOfficeTool && !availability.AnyAvailable;
        HasOfficeWarning = ShowOfficeMissingHint;
        OfficeStatusText = !IsOfficeTool
            ? string.Empty
            : availability.AnyAvailable
                ? _text.Get("Conversion_OfficeFound")
                : _text.Get("Conversion_OfficeMissing");
        OutputFolder = ResolveOutputFolder();
        TotalSizeText = FormatSize(_session.InputPaths.Sum(GetFileLength));
        FileCountText = string.Format(
            CultureInfo.CurrentCulture,
            _text.Get("Conversion_FileCount"),
            _session.InputPaths.Count);
        HasFiles = _session.InputPaths.Count > 0;
        HasNoFiles = !HasFiles;
        NeedMoreFiles = IsMergeTool && _session.InputPaths.Count < 2;
        ShowNeedMoreFiles = NeedMoreFiles;
        NeedMoreFilesText = _text.Get("Conversion_NeedTwoPdfs");
        HasError = false;
        UpdateCanConvert();
        LoadMetadata();
    }

    public void HandleDroppedFiles(IReadOnlyList<string> files)
    {
        AddDroppedFiles(files);
    }

    public void AddDroppedFiles(IReadOnlyList<string> paths)
    {
        if (_session.Converter is null)
        {
            return;
        }

        var allowed = _session.Converter.InputExtensions;
        var accepted = paths.Where(path =>
                allowed.Contains(FileExtensions.Normalize(Path.GetExtension(path)), StringComparer.OrdinalIgnoreCase) &&
                !_session.InputPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (accepted.Count == 0 && paths.Count > 0)
        {
            HasError = true;
            ErrorTitle = _text.Get("Error_UnsupportedFormat");
            ErrorHint = _text.Get("Error_UnsupportedFormat_Hint");
            ErrorDetail = null;
            return;
        }

        foreach (var path in accepted)
        {
            if (_session.Converter.SupportsMultipleInputs || _session.InputPaths.Count == 0)
            {
                _session.InputPaths.Add(path);
            }
            else
            {
                _session.InputPaths.Clear();
                _session.InputPaths.Add(path);
                break;
            }
        }

        RefreshFromSession();
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        if (_session.Converter is null)
        {
            return;
        }

        var files = await _fileDialog.PickFilesAsync(_session.Converter.InputExtensions);
        AddDroppedFiles(files);
    }

    [RelayCommand]
    private async Task ChooseOutputFolderAsync()
    {
        var folder = await _fileDialog.PickFolderAsync();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            OutputFolder = folder;
        }
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (_session.Converter is null || _session.InputPaths.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(OutputFolder);
        var settings = _settingsStore.Current;
        var outputName = DefaultOutputFileName.For(_session.Converter.Id, _session.InputPaths);
        var resolution = OutputFileNameGenerator.Resolve(OutputFolder, outputName, settings.ExistingFilePolicy);

        if (resolution.RequiresUserConfirmation)
        {
            var dialog = new ContentDialog
            {
                Title = _text.Get("AskOverwrite_Title"),
                Content = _text.Get("AskOverwrite_Body"),
                PrimaryButtonText = _text.Get("AskOverwrite_NewName"),
                SecondaryButtonText = _text.Get("AskOverwrite_Overwrite"),
                CloseButtonText = _text.Get("Common_Cancel"),
                XamlRoot = App.MainAppWindow.Content.XamlRoot
            };

            var choice = await dialog.ShowAsync();
            if (choice == ContentDialogResult.None)
            {
                return;
            }

            outputName = choice == ContentDialogResult.Primary
                ? Path.GetFileName(OutputFileNameGenerator.CreateUniquePath(OutputFolder, outputName, File.Exists))
                : outputName;
            settings = new AppSettings
            {
                OutputFolderMode = settings.OutputFolderMode,
                CustomOutputFolder = settings.CustomOutputFolder,
                ExistingFilePolicy = choice == ContentDialogResult.Primary
                    ? ExistingFilePolicy.CreateNewName
                    : ExistingFilePolicy.Overwrite,
                Language = settings.Language,
                OfficeEnginePreference = settings.OfficeEnginePreference
            };
        }
        else
        {
            outputName = Path.GetFileName(resolution.FullPath);
        }

        var options = BuildOptions();
        HasError = false;
        IsProcessing = true;
        CanConvert = false;
        ProgressPercent = 0;
        ProgressText = _text.Get("Progress_Starting");

        _activeJob = _jobQueue.Enqueue(new ConversionJobRequest
        {
            InputFiles = _session.InputPaths.ToList(),
            ConverterId = _session.Converter.Id,
            OutputDirectory = OutputFolder,
            OutputFileName = outputName,
            ExistingFilePolicy = settings.ExistingFilePolicy,
            Options = options
        });
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_activeJob is not null)
        {
            _jobQueue.TryCancel(_activeJob.Id);
        }
    }

    private Dictionary<string, string> BuildOptions()
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (IsSplitTool)
        {
            options[ConversionOptionKeys.SplitMode] = SplitByRange
                ? ConversionOptionKeys.SplitModes.Range
                : ConversionOptionKeys.SplitModes.AllPages;
            options[ConversionOptionKeys.PageRange] = PageRangeText;
        }

        if (IsExtractTool)
        {
            options[ConversionOptionKeys.PageRange] = PageRangeText;
        }

        if (IsReorderTool)
        {
            options[ConversionOptionKeys.PageOrder] = PageOrderText;
        }

        if (IsRotateTool)
        {
            options[ConversionOptionKeys.RotateDegrees] = Rotate270
                ? ConversionOptionKeys.RotateDegreesValues.TwoHundredSeventy
                : Rotate180
                    ? ConversionOptionKeys.RotateDegreesValues.OneHundredEighty
                    : ConversionOptionKeys.RotateDegreesValues.Ninety;
        }

        if (IsOfficeTool)
        {
            options[ConversionOptionKeys.OfficeEngine] = OfficeEngineMicrosoft
                ? nameof(OfficeEnginePreference.MicrosoftOffice)
                : OfficeEngineLibre
                    ? nameof(OfficeEnginePreference.LibreOffice)
                    : nameof(OfficeEnginePreference.Auto);
        }

        return options;
    }

    private void RemoveFile(string path)
    {
        _session.InputPaths.RemoveAll(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
        RefreshFromSession();
    }

    private void MoveFile(string path, int offset)
    {
        var index = _session.InputPaths.FindIndex(item =>
            string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _session.InputPaths.Count)
        {
            return;
        }

        var item = _session.InputPaths[index];
        _session.InputPaths.RemoveAt(index);
        _session.InputPaths.Insert(target, item);
        RebuildFileList();
        UpdateCanConvert();
    }

    private void RebuildFileList()
    {
        InputFiles.Clear();
        for (var index = 0; index < _session.InputPaths.Count; index++)
        {
            var path = _session.InputPaths[index];
            InputFiles.Add(new InputFileItemViewModel(
                path,
                FormatSize(GetFileLength(path)),
                RemoveFile,
                MoveFile,
                canMoveUp: index > 0,
                canMoveDown: index < _session.InputPaths.Count - 1));
        }
    }

    private void UpdateCanConvert()
    {
        if (_session.Converter is null || IsProcessing)
        {
            CanConvert = false;
            return;
        }

        CanConvert = _session.Converter.CanConvert(new ConversionInput { FilePaths = _session.InputPaths });
    }

    private void LoadMetadata()
    {
        MetadataTitle = string.Empty;
        MetadataAuthor = string.Empty;
        MetadataSubject = string.Empty;
        MetadataCreator = string.Empty;
        MetadataProducer = string.Empty;
        if (!IsMetadataTool || _session.InputPaths.Count != 1)
        {
            return;
        }

        if (_metadataReader.TryRead(_session.InputPaths[0], out var metadata))
        {
            MetadataTitle = DisplayOrDash(metadata.Title);
            MetadataAuthor = DisplayOrDash(metadata.Author);
            MetadataSubject = DisplayOrDash(metadata.Subject);
            MetadataCreator = DisplayOrDash(metadata.Creator);
            MetadataProducer = DisplayOrDash(metadata.Producer);
        }
    }

    private void OnJobUpdated(object? sender, ConversionJob job)
    {
        if (_activeJob is null || job.Id != _activeJob.Id)
        {
            return;
        }

        var dispatcher = App.MainAppWindow.DispatcherQueue;
        dispatcher.TryEnqueue(() => ApplyJob(job));
    }

    private void ApplyJob(ConversionJob job)
    {
        ProgressPercent = job.Progress.Percent;
        ProgressText = FormatProgress(job.Progress);

        if (job.Status == JobStatus.Completed)
        {
            IsProcessing = false;
            _session.OutputPaths = job.OutputFiles;
            _session.Duration = (job.CompletedAt - job.StartedAt) ?? TimeSpan.Zero;
            _session.Error = null;
            _navigation.GoCompleted();
        }
        else if (job.Status is JobStatus.Failed or JobStatus.Cancelled)
        {
            IsProcessing = false;
            HasError = true;
            var code = job.Error?.Code ?? ConversionErrorCode.Unknown;
            ErrorTitle = _text.Get(UserErrorMapper.TitleKey(code));
            ErrorHint = _text.Get(UserErrorMapper.HintKey(code));
            ErrorDetail = job.Error?.DeveloperDetail;
            UpdateCanConvert();
        }
    }

    private string ResolveOutputFolder()
    {
        var settings = _settingsStore.Current;
        if (settings.OutputFolderMode == OutputFolderMode.CustomFolder &&
            !string.IsNullOrWhiteSpace(settings.CustomOutputFolder) &&
            Directory.Exists(settings.CustomOutputFolder))
        {
            return settings.CustomOutputFolder;
        }

        if (_session.InputPaths.Count > 0)
        {
            var directory = Path.GetDirectoryName(_session.InputPaths[0]);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return directory;
            }
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private string FormatProgress(ConversionProgress progress)
    {
        if (progress.CurrentPage is int current && progress.TotalPages is int total)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                _text.Get("Progress_Pages"),
                progress.Percent,
                current,
                total);
        }

        return string.Format(CultureInfo.CurrentCulture, _text.Get("Progress_Percent"), progress.Percent);
    }

    private static string DisplayOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    private static long GetFileLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.#} KB";
        }

        return $"{bytes / (1024d * 1024d):0.#} MB";
    }
}

public sealed partial class InputFileItemViewModel : ObservableObject
{
    public InputFileItemViewModel(
        string path,
        string sizeText,
        Action<string> remove,
        Action<string, int> move,
        bool canMoveUp,
        bool canMoveDown)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        SizeText = sizeText;
        CanMoveUp = canMoveUp;
        CanMoveDown = canMoveDown;
        RemoveCommand = new RelayCommand(() => remove(path));
        MoveUpCommand = new RelayCommand(() => move(path, -1), () => canMoveUp);
        MoveDownCommand = new RelayCommand(() => move(path, 1), () => canMoveDown);
    }

    public string Path { get; }

    public string Name { get; }

    public string SizeText { get; }

    public bool CanMoveUp { get; }

    public bool CanMoveDown { get; }

    public IRelayCommand RemoveCommand { get; }

    public IRelayCommand MoveUpCommand { get; }

    public IRelayCommand MoveDownCommand { get; }
}
