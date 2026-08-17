using LocalConvert.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace LocalConvert.App.Views;

public sealed partial class ConversionPage : Page
{
    public ConversionPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ConversionViewModel>();
    }

    public ConversionViewModel ViewModel { get; }

    public Visibility ToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.RefreshFromSession();
        base.OnNavigatedTo(e);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Detach();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var files = items.OfType<StorageFile>().Select(file => file.Path).ToList();
        if (files.Count > 0)
        {
            ViewModel.AddDroppedFiles(files);
        }
    }
}
