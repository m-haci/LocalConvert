using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using LocalConvert.App.ViewModels;
using LocalConvert.Core.Conversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace LocalConvert.App.Views;

public sealed partial class PdfToolsPage : Page
{
    public PdfToolsPage()
    {
        InitializeComponent();
        ViewModel = new ToolListViewModel(
            App.Services.GetRequiredService<IConverterCatalog>(),
            App.Services.GetRequiredService<ConversionSessionState>(),
            App.Services.GetRequiredService<IAppNavigation>(),
            App.Services.GetRequiredService<IUiText>(),
            ConverterIds.PdfMerge,
            ConverterIds.PdfSplit,
            ConverterIds.PdfExtract,
            ConverterIds.PdfRotate,
            ConverterIds.PdfReorder,
            ConverterIds.PdfMetadata);
    }

    public ToolListViewModel ViewModel { get; }
}
