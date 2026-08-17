namespace LocalConvert.Core.Conversion;

public interface IConverterCatalog
{
    IReadOnlyList<IFileConverter> All { get; }

    IFileConverter? GetById(string converterId);

    IReadOnlyList<IFileConverter> GetConvertersFor(IReadOnlyList<string> filePaths);
}
