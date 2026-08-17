using FluentAssertions;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Office;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class OfficeEngineResolverTests
{
    [Fact]
    public void Auto_PrefersMicrosoftWhenTheMatchingAppExists()
    {
        var availability = new OfficeAvailabilityResult
        {
            WordAvailable = true,
            LibreOfficeAvailable = true,
            LibreOfficeExecutablePath = @"C:\soffice.exe"
        };

        OfficeEngineResolver.Resolve(OfficeEnginePreference.Auto, availability, ConverterIds.WordToPdf)
            .Should()
            .Be(OfficeEngineKind.MicrosoftOffice);
    }

    [Fact]
    public void Auto_UsesLibreOfficeWhenTheMatchingAppIsMissing()
    {
        var availability = new OfficeAvailabilityResult
        {
            ExcelAvailable = true,
            LibreOfficeAvailable = true,
            LibreOfficeExecutablePath = @"C:\soffice.exe"
        };

        OfficeEngineResolver.Resolve(OfficeEnginePreference.Auto, availability, ConverterIds.WordToPdf)
            .Should()
            .Be(OfficeEngineKind.LibreOffice);
    }

    [Fact]
    public void MissingEngines_ReturnNone()
    {
        var availability = new OfficeAvailabilityResult();

        OfficeEngineResolver.Resolve(OfficeEnginePreference.Auto, availability, ConverterIds.ExcelToPdf)
            .Should()
            .Be(OfficeEngineKind.None);
    }
}
