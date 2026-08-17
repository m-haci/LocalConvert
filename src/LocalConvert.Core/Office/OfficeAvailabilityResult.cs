using LocalConvert.Core.Conversion;

namespace LocalConvert.Core.Office;

public sealed class OfficeAvailabilityResult
{
    public bool WordAvailable { get; init; }

    public bool ExcelAvailable { get; init; }

    public bool PowerPointAvailable { get; init; }

    public bool LibreOfficeAvailable { get; init; }

    public string? LibreOfficeExecutablePath { get; init; }

    public bool MicrosoftOfficeAvailable => WordAvailable || ExcelAvailable || PowerPointAvailable;

    public bool AnyAvailable => MicrosoftOfficeAvailable || LibreOfficeAvailable;

    public bool IsMicrosoftAvailableFor(string converterId)
    {
        return converterId switch
        {
            ConverterIds.WordToPdf => WordAvailable,
            ConverterIds.ExcelToPdf => ExcelAvailable,
            ConverterIds.PowerPointToPdf => PowerPointAvailable,
            _ => MicrosoftOfficeAvailable
        };
    }
}
