using SET09102.Administrator.Pages;

namespace SET09102;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(UserManagementPage), typeof(UserManagementPage));
        Routing.RegisterRoute(nameof(SensorManagementPage), typeof(SensorManagementPage));
        Routing.RegisterRoute(nameof(SensorConfigPage), typeof(SensorConfigPage));
        Routing.RegisterRoute(nameof(DataManagementPage), typeof(DataManagementPage));
        Routing.RegisterRoute(nameof(AuditLogPage), typeof(AuditLogPage));
    }
}