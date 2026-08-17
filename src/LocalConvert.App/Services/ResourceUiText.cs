using Windows.ApplicationModel.Resources;

namespace LocalConvert.App.Services;

public sealed class ResourceUiText : IUiText
{
    private ResourceLoader? _loader;

    public string Get(string key)
    {
        try
        {
            _loader ??= ResourceLoader.GetForViewIndependentUse();
            var value = _loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch (Exception)
        {
            return key;
        }
    }
}
