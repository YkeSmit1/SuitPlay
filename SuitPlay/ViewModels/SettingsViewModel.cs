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
        DeveloperMode = Preferences.Get(Constants.DeveloperMode, true);
        OnlyLinesInSuitPlay = Preferences.Get(Constants.OnlyLinesInSuitPlay, true);
        OnlyCombinationsInSuitPlay = Preferences.Get(Constants.OnlyCombinationsInSuitPlay, true);
        MaxLinesInCalculate = Preferences.Get(Constants.MaxLinesInCalculate, 10000);
        MaxLinesInDistributions = Preferences.Get(Constants.MaxLinesInDistributions, 5);
    }

    public void Save()
    {
        Preferences.Set(Constants.DeveloperMode, DeveloperMode);
        Preferences.Set(Constants.OnlyLinesInSuitPlay, OnlyLinesInSuitPlay);
        Preferences.Set(Constants.OnlyCombinationsInSuitPlay, OnlyCombinationsInSuitPlay);
        Preferences.Set(Constants.MaxLinesInCalculate, MaxLinesInCalculate);
        Preferences.Set(Constants.MaxLinesInDistributions, MaxLinesInDistributions);
    }
}