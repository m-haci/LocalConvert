using LocalConvert.Core.Conversion;

namespace LocalConvert.Core.Suggestions;

public sealed class OperationSuggester
{
    private readonly IConverterCatalog _catalog;

    public OperationSuggester(IConverterCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<SuggestedOperation> Suggest(IReadOnlyList<string> filePaths)
    {
        return _catalog.GetConvertersFor(filePaths)
            .Select(converter => new SuggestedOperation
            {
                ConverterId = converter.Id,
                DisplayNameKey = converter.DisplayNameKey
            })
            .ToList();
    }
}
