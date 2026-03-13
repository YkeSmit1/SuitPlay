using SuitPlay.Services;
using SuitPlay.ViewModels;

namespace SuitPlay.Pages;

public partial class FilterPage : ContentPage
{
    private readonly SettingsService settingsService;
    
    public FilterPage(SettingsService settingsService)
    {
        this.settingsService = settingsService;
        InitializeComponent();
    }
    
    protected override void OnDisappearing()
    {
        var filterViewModel = (FilterViewModel)BindingContext;
        filterViewModel.Save();
        settingsService.NotifySettingsChanged();
    }
}