using LocalConvert.Core.Conversion;

namespace LocalConvert.App.Services;

public sealed class ConversionSessionState
{
    public IFileConverter? Converter { get; set; }

    public List<string> InputPaths { get; } = [];

    public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> OutputPaths { get; set; } = [];

    public TimeSpan Duration { get; set; }

    public ConversionError? Error { get; set; }

    public void Reset()
    {
        Converter = null;
        InputPaths.Clear();
        Options.Clear();
        OutputPaths = [];
        Duration = TimeSpan.Zero;
        Error = null;
    }

    public void Start(IFileConverter converter, IEnumerable<string> paths)
    {
        Reset();
        Converter = converter;
        InputPaths.AddRange(paths);
    }
}
