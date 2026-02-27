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
    public int MaxLinesInCalculate { get; set; }
    public int MaxLinesInDistributions { get; set; }

    private void Load()
    {
        DeveloperMode = Preferences.Get("DeveloperMode", true);
        OnlyLinesInSuitPlay = Preferences.Get("OnlyLinesInSuitPlay", true);
        OnlyCombinationsInSuitPlay = Preferences.Get("OnlyCombinationsInSuitPlay", true);
        MaxLinesInCalculate = Preferences.Get("MaxLinesInCalculate", 10000);
        MaxLinesInDistributions = Preferences.Get("MaxLinesInDistributions", 5);
    }

    public void Save()
    {
        Preferences.Set("DeveloperMode", DeveloperMode);
        Preferences.Set("OnlyLinesInSuitPlay", OnlyLinesInSuitPlay);
        Preferences.Set("OnlyCombinationsInSuitPlay", OnlyCombinationsInSuitPlay);
        Preferences.Set("MaxLinesInCalculate", MaxLinesInCalculate);
        Preferences.Set("MaxLinesInDistributions", MaxLinesInDistributions);
    }
}