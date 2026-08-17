namespace LocalConvert.Core.Conversion;

public sealed class ConverterCatalog : IConverterCatalog
{
    private readonly IReadOnlyList<IFileConverter> _converters;

    public ConverterCatalog(IEnumerable<IFileConverter> converters)
    {
        _converters = converters.ToList();
    }

    public IReadOnlyList<IFileConverter> All => _converters;

    public IFileConverter? GetById(string converterId)
    {
        return _converters.FirstOrDefault(converter =>
            string.Equals(converter.Id, converterId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IFileConverter> GetConvertersFor(IReadOnlyList<string> filePaths)
    {
        if (filePaths.Count == 0)
        {
            return [];
        }

        var input = new ConversionInput { FilePaths = filePaths };
        return _converters.Where(converter => converter.CanConvert(input)).ToList();
    }
}
