using Microsoft.Extensions.Logging;
using Serilog;

namespace SuitPlay;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        SetupLogging();

        return builder.Build();
    }
    
    private static void SetupLogging()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
    
}