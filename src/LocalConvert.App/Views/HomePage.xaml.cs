using LocalConvert.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace LocalConvert.App.Views;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
    }

    public HomeViewModel ViewModel { get; }

    public Visibility ToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
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
            ViewModel.HandleFiles(files);
        }
    }
}
