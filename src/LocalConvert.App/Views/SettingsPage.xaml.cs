using LocalConvert.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LocalConvert.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }

    public SettingsViewModel ViewModel { get; }

    private async void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SaveCommand.CanExecute(null))
        {
            await ViewModel.SaveCommand.ExecuteAsync(null);
        }
    }
}
