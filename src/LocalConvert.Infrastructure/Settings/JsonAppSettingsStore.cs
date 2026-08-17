using System.Text.Json;
using LocalConvert.Core.Settings;

namespace LocalConvert.Infrastructure.Settings;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsFilePath;
    private readonly object _sync = new();
    private AppSettings _current = new();

    public JsonAppSettingsStore(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public AppSettings Current
    {
        get
        {
            lock (_sync)
            {
                return Clone(_current);
            }
        }
    }

    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!File.Exists(_settingsFilePath))
            {
                _current = new AppSettings();
                return Task.FromResult(Clone(_current));
            }

            var json = File.ReadAllText(_settingsFilePath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
            return Task.FromResult(Clone(_current));
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(settings);

        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(_settingsFilePath, json);
            _current = Clone(settings);
        }

        return Task.CompletedTask;
    }

    private static AppSettings Clone(AppSettings settings)
    {
        return new AppSettings
        {
            OutputFolderMode = settings.OutputFolderMode,
            CustomOutputFolder = settings.CustomOutputFolder,
            ExistingFilePolicy = settings.ExistingFilePolicy,
            Language = settings.Language,
            OfficeEnginePreference = settings.OfficeEnginePreference
        };
    }
}
