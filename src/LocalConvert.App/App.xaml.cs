using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using LocalConvert.App.Views;
using LocalConvert.Core.Files;
using LocalConvert.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace LocalConvert.App;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public App()
    {
        UnhandledException += OnUnhandledException;
        try
        {
            Program.WriteStartupLog("App constructor: InitializeComponent.");
            InitializeComponent();
            Program.WriteStartupLog("App constructor: building services.");
            Services = AppHost.Build();
            Program.WriteStartupLog("App constructor: ready.");
        }
        catch (Exception exception)
        {
            Program.ShowError("LocalConvert", exception);
            throw;
        }
    }

    public static IServiceProvider Services { get; private set; } = default!;

    public static Window MainAppWindow { get; private set; } = default!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Program.WriteStartupLog("OnLaunched.");
            var settingsStore = Services.GetRequiredService<IAppSettingsStore>();
            var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
            TrySetLanguage(settings.Language);

            Services.GetRequiredService<ITemporaryFileService>().CleanupOrphanedDirectories();

            _mainWindow = Services.GetRequiredService<MainWindow>();
            MainAppWindow = _mainWindow;
            _mainWindow.Activate();
            Services.GetRequiredService<IAppNavigation>().Attach(_mainWindow);
            Program.WriteStartupLog("OnLaunched: window activated.");
        }
        catch (Exception exception)
        {
            Program.ShowError("LocalConvert", exception);
        }
    }

    private static void TrySetLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return;
        }

        try
        {
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;
        }
        catch (Exception exception)
        {
            Program.WriteStartupLog("Language override skipped: " + exception.Message);
        }
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        Program.ShowError("LocalConvert", args.Exception);
        args.Handled = true;
    }
}
