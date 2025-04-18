using SET09102.Administrator.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class AuditLogPage : ContentPage
    {
        private readonly AuditService _auditService;
        public ObservableCollection<AuditLog> AuditLogs { get; set; }
        public List<string> EventTypes { get; set; }
        public string SelectedEventType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ICommand FilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public AuditLogPage(AuditService auditService)
        {
            InitializeComponent();
            _auditService = auditService;
            AuditLogs = new ObservableCollection<AuditLog>();
            EventTypes = new List<string> 
            { 
                "All",
                "Login",
                "Logout",
                "PasswordChange",
                "UserCreated",
                "UserDeleted",
                "RoleChanged",
                "SensorConfig",
                "FirmwareUpdate",
                "DataExport",
                "DataImport"
            };

            FilterCommand = new Command(async () => await LoadAuditLogs());
            ClearFilterCommand = new Command(async () => await ClearFilters());

            LoadAuditLogs();
        }

        private async Task LoadAuditLogs()
        {
            try
            {
                var logs = await _auditService.GetAuditLogsAsync(StartDate, EndDate);
                
                if (!string.IsNullOrEmpty(SelectedEventType) && SelectedEventType != "All")
                {
                    logs = logs.Where(l => l.EventType == SelectedEventType).ToList();
                }

                AuditLogs.Clear();
                foreach (var log in logs)
                {
                    AuditLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load audit logs: " + ex.Message, "OK");
            }
        }

        private async Task ClearFilters()
        {
            StartDate = null;
            EndDate = null;
            SelectedEventType = "All";
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            OnPropertyChanged(nameof(SelectedEventType));
            await LoadAuditLogs();
        }
    }
} 