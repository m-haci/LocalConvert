using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using LocalConvert.App.ViewModels;
using LocalConvert.Core.Conversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace LocalConvert.App.Views;

public sealed partial class ConvertPage : Page
{
    public ConvertPage()
    {
        InitializeComponent();
        ViewModel = new ToolListViewModel(
            App.Services.GetRequiredService<IConverterCatalog>(),
            App.Services.GetRequiredService<ConversionSessionState>(),
            App.Services.GetRequiredService<IAppNavigation>(),
            App.Services.GetRequiredService<IUiText>(),
            ConverterIds.ImageToPdf,
            ConverterIds.WordToPdf,
            ConverterIds.PowerPointToPdf,
            ConverterIds.ExcelToPdf,
            ConverterIds.PdfToJpeg,
            ConverterIds.PdfToPng);
    }

    public ToolListViewModel ViewModel { get; }
}
