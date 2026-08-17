using LocalConvert.Core.Files;
using LocalConvert.Core.Office;

namespace LocalConvert.Core.Settings;

public sealed class AppSettings
{
    public OutputFolderMode OutputFolderMode { get; set; } = OutputFolderMode.SameAsSource;

    public string? CustomOutputFolder { get; set; }

    public ExistingFilePolicy ExistingFilePolicy { get; set; } = ExistingFilePolicy.CreateNewName;

    public string Language { get; set; } = "tr-TR";

    public OfficeEnginePreference OfficeEnginePreference { get; set; } = OfficeEnginePreference.Auto;
}
