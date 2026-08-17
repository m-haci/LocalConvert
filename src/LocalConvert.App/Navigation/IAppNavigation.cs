using LocalConvert.App.Views;
using Microsoft.UI.Xaml.Controls;

namespace LocalConvert.App.Navigation;

public interface IAppNavigation
{
    void Attach(MainWindow window);

    void NavigateTo(Type pageType);

    void GoHome();

    void GoConvert();

    void GoPdfTools();

    void GoImages();

    void GoSettings();

    void GoConversion();

    void GoCompleted();
}
