using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalConvert.App.Navigation;
using LocalConvert.App.Services;
using LocalConvert.Core.Conversion;

namespace LocalConvert.App.ViewModels;

public sealed class ToolListViewModel : ObservableObject
{
    public ToolListViewModel(
        IConverterCatalog catalog,
        ConversionSessionState session,
        IAppNavigation navigation,
        IUiText text,
        params string[] converterIds)
    {
        Tools = new ObservableCollection<ToolItemViewModel>(
            converterIds
                .Select(catalog.GetById)
                .Where(converter => converter is not null)
                .Select(converter => ToolItemViewModel.Create(
                    converter!,
                    text,
                    () =>
                    {
                        session.Start(converter!, []);
                        navigation.GoConversion();
                    })));
    }

    public ObservableCollection<ToolItemViewModel> Tools { get; }
}
