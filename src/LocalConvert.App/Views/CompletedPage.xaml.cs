using LocalConvert.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LocalConvert.App.Views;

public sealed partial class CompletedPage : Page
{
    public CompletedPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CompletedViewModel>();
    }

    public CompletedViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.Refresh();
        base.OnNavigatedTo(e);
    }
}
