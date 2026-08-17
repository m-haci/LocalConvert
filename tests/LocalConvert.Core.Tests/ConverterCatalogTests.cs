using FluentAssertions;
using LocalConvert.Core.Conversion;
using Xunit;

namespace LocalConvert.Core.Tests;

public sealed class ConverterCatalogTests
{
    [Fact]
    public void GetConvertersFor_ReturnsOnlyMatchingConverters()
    {
        var imageConverter = new FakeConverter(ConverterIds.ImageToPdf, [".jpg", ".png"], allowMultiple: true);
        var mergeConverter = new FakeConverter(ConverterIds.PdfMerge, [".pdf"], allowMultiple: true, minimumFiles: 2);
        var splitConverter = new FakeConverter(ConverterIds.PdfSplit, [".pdf"], allowMultiple: false, minimumFiles: 1, maximumFiles: 1);
        var catalog = new ConverterCatalog([imageConverter, mergeConverter, splitConverter]);

        catalog.GetConvertersFor([@"C:\a.jpg"]).Select(c => c.Id).Should().Equal(ConverterIds.ImageToPdf);
        catalog.GetConvertersFor([@"C:\a.pdf"]).Select(c => c.Id).Should().Equal(ConverterIds.PdfSplit);
        catalog.GetConvertersFor([@"C:\a.pdf", @"C:\b.pdf"]).Select(c => c.Id).Should().Equal(ConverterIds.PdfMerge);
        catalog.GetConvertersFor([@"C:\a.docx"]).Should().BeEmpty();
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        var converter = new FakeConverter(ConverterIds.PdfMerge, [".pdf"], true, 2);
        var catalog = new ConverterCatalog([converter]);

        catalog.GetById("PDF-MERGE").Should().BeSameAs(converter);
    }

    private sealed class FakeConverter : IFileConverter
    {
        private readonly int _minimumFiles;
        private readonly int _maximumFiles;

        public FakeConverter(
            string id,
            IReadOnlyList<string> inputExtensions,
            bool allowMultiple,
            int minimumFiles = 1,
            int maximumFiles = int.MaxValue)
        {
            Id = id;
            InputExtensions = inputExtensions;
            SupportsMultipleInputs = allowMultiple;
            _minimumFiles = minimumFiles;
            _maximumFiles = maximumFiles;
        }

        public string Id { get; }

        public string DisplayNameKey => Id;

        public string DescriptionKey => Id;

        public string Glyph => "\uE8A5";

        public ConverterCategory Category => ConverterCategory.Convert;

        public IReadOnlyList<string> InputExtensions { get; }

        public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

        public bool SupportsMultipleInputs { get; }

        public bool CanConvert(ConversionInput input)
        {
            if (input.FilePaths.Count < _minimumFiles || input.FilePaths.Count > _maximumFiles)
            {
                return false;
            }

            return input.Extensions.All(extension =>
                InputExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        public Task<ConversionResult> ConvertAsync(
            ConversionRequest request,
            IProgress<ConversionProgress>? progress,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ConversionResult.Succeeded([], TimeSpan.Zero));
        }
    }
}
