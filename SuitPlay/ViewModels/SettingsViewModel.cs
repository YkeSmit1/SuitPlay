using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel()
    {
        Load();
    }

    [ObservableProperty] public partial bool DeveloperMode { get; set; }
    public bool OnlyLinesInSuitPlay { get; set; }
    public bool OnlyCombinationsInSuitPlay { get; set; }

    private void Load()
    {
        DeveloperMode = Preferences.Get("DeveloperMode", true);
        OnlyLinesInSuitPlay = Preferences.Get("OnlyLinesInSuitPlay", true);
        OnlyCombinationsInSuitPlay = Preferences.Get("OnlyCombinationsInSuitPlay", true);
    }

    public void Save()
    {
        Preferences.Set("DeveloperMode", DeveloperMode);
        Preferences.Set("OnlyLinesInSuitPlay", OnlyLinesInSuitPlay);
        Preferences.Set("OnlyCombinationsInSuitPlay", OnlyCombinationsInSuitPlay);
    }
}