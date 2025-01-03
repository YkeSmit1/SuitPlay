namespace SuitPlay;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }
    
    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = new Window(new AppShell());

#if WINDOWS
        window.Width = 800;
        window.Height = 1000;
        
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
#endif
        return window;
    }    
}