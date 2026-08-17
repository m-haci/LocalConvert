namespace LocalConvert.Core.Office;

public enum OfficeEngineKind
{
    None = 0,
    MicrosoftOffice = 1,
    LibreOffice = 2
}

public static class OfficeEngineResolver
{
    public static OfficeEngineKind Resolve(
        OfficeEnginePreference preference,
        OfficeAvailabilityResult availability,
        string? converterId = null)
    {
        ArgumentNullException.ThrowIfNull(availability);

        var microsoftAvailable = string.IsNullOrWhiteSpace(converterId)
            ? availability.MicrosoftOfficeAvailable
            : availability.IsMicrosoftAvailableFor(converterId);

        return preference switch
        {
            OfficeEnginePreference.MicrosoftOffice => microsoftAvailable
                ? OfficeEngineKind.MicrosoftOffice
                : OfficeEngineKind.None,
            OfficeEnginePreference.LibreOffice => availability.LibreOfficeAvailable
                ? OfficeEngineKind.LibreOffice
                : OfficeEngineKind.None,
            _ => microsoftAvailable
                ? OfficeEngineKind.MicrosoftOffice
                : availability.LibreOfficeAvailable
                    ? OfficeEngineKind.LibreOffice
                    : OfficeEngineKind.None
        };
    }
}
