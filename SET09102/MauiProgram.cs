using Microsoft.Extensions.Logging;
using SET09102.Administrator.Pages;
using SET09102.Administrator.Services;
using SET09102.Services.Administration;

namespace SET09102;

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

        // Register services
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AuditService>();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSingleton<SensorService>();
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<DataManagementService>();
        builder.Services.AddSingleton<SecurityService>();

        // Register pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<SensorManagementPage>();
        builder.Services.AddTransient<SensorConfigPage>();
        builder.Services.AddTransient<DataManagementPage>();
        builder.Services.AddTransient<AuditLogPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}