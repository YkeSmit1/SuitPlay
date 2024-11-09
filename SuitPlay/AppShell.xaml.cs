using SuitPlay.Pages;

namespace SuitPlay;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(OverviewPage), typeof(OverviewPage));
    }
}