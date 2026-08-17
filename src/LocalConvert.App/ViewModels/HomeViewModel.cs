using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Suggestions;

namespace LocalConvert.App.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IConverterCatalog _catalog;
    private readonly OperationSuggester _suggester;
    private readonly ConversionSessionState _session;
    private readonly IAppNavigation _navigation;
    private readonly IFileDialogService _fileDialog;
    private readonly IUiText _text;

    public HomeViewModel(
        IConverterCatalog catalog,
        OperationSuggester suggester,
        ConversionSessionState session,
        IAppNavigation navigation,
        IFileDialogService fileDialog,
        IUiText text)
    {
        _catalog = catalog;
        _suggester = suggester;
        _session = session;
        _navigation = navigation;
        _fileDialog = fileDialog;
        _text = text;
        PdfTools = CreateGroup(ConverterCategory.Pdf);
        ConvertTools = CreateGroup(ConverterCategory.Convert);
        ImageTools = CreateGroup(ConverterCategory.Images);
    }

    public ObservableCollection<ToolItemViewModel> PdfTools { get; }

    public ObservableCollection<ToolItemViewModel> ConvertTools { get; }

    public ObservableCollection<ToolItemViewModel> ImageTools { get; }

    public ObservableCollection<ToolItemViewModel> SuggestedTools { get; } = [];

    [ObservableProperty]
    private bool hasSuggestions;

    [ObservableProperty]
    private bool hasUnsupportedFiles;

    [RelayCommand]
    private async Task ChooseFilesAsync()
    {
        var files = await _fileDialog.PickFilesAsync(FileExtensions.AllSupported);
        if (files.Count > 0)
        {
            HandleFiles(files);
        }
    }

    [RelayCommand]
    private void OpenTool(string converterId)
    {
        var converter = _catalog.GetById(converterId);
        if (converter is null)
        {
            return;
        }

        _session.Start(converter, []);
        _navigation.GoConversion();
    }

    public void HandleFiles(IReadOnlyList<string> files)
    {
        SuggestedTools.Clear();
        HasUnsupportedFiles = false;
        var suggestions = _suggester.Suggest(files);
        foreach (var suggestion in suggestions)
        {
            var converter = _catalog.GetById(suggestion.ConverterId);
            if (converter is null)
            {
                continue;
            }

            SuggestedTools.Add(ToolItemViewModel.Create(
                converter,
                _text,
                () =>
                {
                    _session.Start(converter, files);
                    _navigation.GoConversion();
                }));
        }

        HasSuggestions = SuggestedTools.Count > 0;
        HasUnsupportedFiles = files.Count > 0 && SuggestedTools.Count == 0;

        if (SuggestedTools.Count == 1)
        {
            SuggestedTools[0].SelectCommand.Execute(null);
        }
    }

    private ObservableCollection<ToolItemViewModel> CreateGroup(ConverterCategory category)
    {
        return new ObservableCollection<ToolItemViewModel>(
            _catalog.All
                .Where(converter => converter.Category.HasFlag(category))
                .Select(converter => ToolItemViewModel.Create(
                    converter,
                    _text,
                    () => OpenTool(converter.Id))));
    }
}

public sealed partial class ToolItemViewModel : ObservableObject
{
    private readonly Action _onSelect;

    public ToolItemViewModel(string converterId, string title, string description, string glyph, Action onSelect)
    {
        ConverterId = converterId;
        Title = title;
        Description = description;
        Glyph = glyph;
        _onSelect = onSelect;
    }

    public string ConverterId { get; }

    public string Title { get; }

    public string Description { get; }

    public string Glyph { get; }

    [RelayCommand]
    private void Select() => _onSelect();

    public static ToolItemViewModel Create(IFileConverter converter, IUiText text, Action onSelect)
    {
        return new ToolItemViewModel(
            converter.Id,
            text.Get(converter.DisplayNameKey),
            text.Get(converter.DescriptionKey),
            converter.Glyph,
            onSelect);
    }
}
