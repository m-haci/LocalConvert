using LocalConvert.Core.Paths;

namespace LocalConvert.Infrastructure.Paths;

public sealed class WindowsAppPaths : IAppPaths
{
    public WindowsAppPaths()
    {
        AppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalConvert");
        TempRoot = Path.Combine(AppDataRoot, "Temp");
        LogsRoot = Path.Combine(AppDataRoot, "Logs");
        SettingsFilePath = Path.Combine(AppDataRoot, "settings.json");
    }

    public WindowsAppPaths(string appDataRoot)
    {
        AppDataRoot = appDataRoot;
        TempRoot = Path.Combine(AppDataRoot, "Temp");
        LogsRoot = Path.Combine(AppDataRoot, "Logs");
        SettingsFilePath = Path.Combine(AppDataRoot, "settings.json");
    }

    public string AppDataRoot { get; }

    public string TempRoot { get; }

    public string LogsRoot { get; }

    public string SettingsFilePath { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(TempRoot);
        Directory.CreateDirectory(LogsRoot);
    }
}
