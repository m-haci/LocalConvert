using FluentAssertions;
using LocalConvert.Core.Files;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class OutputFileNameGeneratorTests
{
    [Fact]
    public void Resolve_ReturnsOriginalName_WhenFileDoesNotExist()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = OutputFileNameGenerator.Resolve(
            @"C:\out",
            "document.pdf",
            ExistingFilePolicy.CreateNewName,
            path => existing.Contains(path));

        result.FullPath.Should().Be(@"C:\out\document.pdf");
        result.RequiresUserConfirmation.Should().BeFalse();
        result.AlreadyExists.Should().BeFalse();
    }

    [Fact]
    public void Resolve_CreatesNumberedName_WhenFileExists()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\out\document.pdf",
            @"C:\out\document (1).pdf"
        };

        var result = OutputFileNameGenerator.Resolve(
            @"C:\out",
            "document.pdf",
            ExistingFilePolicy.CreateNewName,
            path => existing.Contains(path));

        result.FullPath.Should().Be(@"C:\out\document (2).pdf");
        result.RequiresUserConfirmation.Should().BeFalse();
    }

    [Fact]
    public void Resolve_Ask_RequiresConfirmation_WhenFileExists()
    {
        var result = OutputFileNameGenerator.Resolve(
            @"C:\out",
            "document.pdf",
            ExistingFilePolicy.Ask,
            _ => true);

        result.RequiresUserConfirmation.Should().BeTrue();
        result.FullPath.Should().Be(@"C:\out\document.pdf");
    }

    [Fact]
    public void Resolve_Overwrite_KeepsOriginalPath()
    {
        var result = OutputFileNameGenerator.Resolve(
            @"C:\out",
            "document.pdf",
            ExistingFilePolicy.Overwrite,
            _ => true);

        result.FullPath.Should().Be(@"C:\out\document.pdf");
        result.RequiresUserConfirmation.Should().BeFalse();
        result.AlreadyExists.Should().BeTrue();
    }
}
