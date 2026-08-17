using LocalConvert.App.Navigation;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT.Interop;

namespace LocalConvert.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly IAppNavigation _navigation;
    private bool _suppressSelection;

    public MainWindow(IAppNavigation navigation)
    {
        _navigation = navigation;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SystemBackdrop = new MicaBackdrop();
        ApplyWindowIcon();
    }

    public Frame ContentFrame => ShellFrame;

    private void ApplyWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LocalConvert.ico");
        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "LocalConvert.png");
        if (File.Exists(iconPath))
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(iconPath);
        }

        if (File.Exists(imagePath))
        {
            TitleBarIcon.Source = new BitmapImage(new Uri(imagePath));
        }
    }

    public void SelectHome()
    {
        _suppressSelection = true;
        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
        _suppressSelection = false;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_suppressSelection)
        {
            return;
        }        
        if (args.IsSettingsSelected)
        {
            _navigation.GoSettings();
            return;
        }

        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "home":
                    _navigation.GoHome();
                    break;
                case "convert":
                    _navigation.GoConvert();
                    break;
                case "pdfTools":
                    _navigation.GoPdfTools();
                    break;
                case "images":
                    _navigation.GoImages();
                    break;
            }
        }
    }
}
