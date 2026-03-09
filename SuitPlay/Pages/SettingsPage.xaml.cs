using SuitPlay.Services;
using SuitPlay.ViewModels;

namespace SuitPlay.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService settingsService;
    
    public SettingsPage(SettingsService settingsService)
    {
        this.settingsService = settingsService;
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        var settingsViewModel = (SettingsViewModel)BindingContext;
        settingsViewModel.Save();
        settingsService.NotifySettingsChanged();
    }
}