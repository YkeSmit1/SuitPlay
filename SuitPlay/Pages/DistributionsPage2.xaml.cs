using Serilog.Core;
using SuitPlay.ViewModels;

namespace SuitPlay.Pages;

public partial class DistributionsPage2 : ContentPage
{
    public DistributionsPage2()
    {
        InitializeComponent();
    }

    private async void Button_OnClicked(object sender, EventArgs e)
    {
        try
        {
            await ((Distributions2ViewModel)BindingContext).ExportViewModelToCsv();
        }
        catch (Exception exception)
        {
            await Shell.Current.DisplayAlert("Error", exception.Message, "Ok");
        }
    }
}