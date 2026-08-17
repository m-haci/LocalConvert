namespace LocalConvert.Core.Paths;

public interface IAppPaths
{
    string AppDataRoot { get; }

    string TempRoot { get; }

    string LogsRoot { get; }

    string SettingsFilePath { get; }
}
