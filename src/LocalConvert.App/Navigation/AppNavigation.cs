using LocalConvert.App.Views;
using Microsoft.UI.Xaml.Controls;

namespace LocalConvert.App.Navigation;

public sealed class AppNavigation : IAppNavigation
{
    private Frame? _frame;

    public void Attach(MainWindow window)
    {
        _frame = window.ContentFrame;
        GoHome();
        window.SelectHome();
    }

    public void NavigateTo(Type pageType)
    {
        if (_frame is null)
        {
            return;
        }

        if (_frame.CurrentSourcePageType != pageType)
        {
            _frame.Navigate(pageType);
        }
    }

    public void GoHome() => NavigateTo(typeof(HomePage));

    public void GoConvert() => NavigateTo(typeof(ConvertPage));

    public void GoPdfTools() => NavigateTo(typeof(PdfToolsPage));

    public void GoImages() => NavigateTo(typeof(ImagesPage));

    public void GoSettings() => NavigateTo(typeof(SettingsPage));

    public void GoConversion() => NavigateTo(typeof(ConversionPage));

    public void GoCompleted() => NavigateTo(typeof(CompletedPage));
}
