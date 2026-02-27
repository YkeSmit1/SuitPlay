using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuitPlay.ViewModels;

namespace SuitPlay.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        var settingsViewModel = (SettingsViewModel)BindingContext;
        settingsViewModel.Save();
    }
}