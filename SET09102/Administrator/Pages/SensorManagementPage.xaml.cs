using SET09102.Administrator.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class SensorManagementPage : ContentPage, INotifyPropertyChanged
    {
        private readonly SensorService _sensorService;
        public ObservableCollection<Sensor> Sensors { get; set; }

        public ICommand ConfigureCommand { get; }
        public ICommand UpdateFirmwareCommand { get; }
        public ICommand ScheduleMaintenanceCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ToggleCommand { get; }

        public SensorManagementPage(SensorService sensorService)
        {
            InitializeComponent();
            _sensorService = sensorService;
            Sensors = new ObservableCollection<Sensor>();

            ConfigureCommand = new Command<Sensor>(async (sensor) => await ConfigureSensor(sensor));
            UpdateFirmwareCommand = new Command<Sensor>(async (sensor) => await UpdateFirmware(sensor));
            ScheduleMaintenanceCommand = new Command<Sensor>(async (sensor) => await ScheduleMaintenance(sensor));
            EditCommand = new Command<Sensor>(async (sensor) => await EditSensor(sensor));
            ToggleCommand = new Command<Sensor>(async (sensor) => await ToggleSensor(sensor));

            LoadSensors();
        }

        private async void LoadSensors()
        {
            try
            {
                var sensors = await _sensorService.GetAllSensorsAsync();
                Sensors.Clear();
                foreach (var sensor in sensors)
                {
                    Sensors.Add(sensor);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load sensors: " + ex.Message, "OK");
            }
        }

        private async Task ConfigureSensor(Sensor sensor)
        {
            if (sensor == null) return;

            var result = await DisplayPromptAsync(
                "Configure Sensor",
                "Enter new configuration (JSON format):",
                initialValue: sensor.Configuration
            );

            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    await _sensorService.UpdateSensorConfigurationAsync(sensor.Id, result);
                    await DisplayAlert("Success", "Sensor configuration updated", "OK");
                    LoadSensors();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to update configuration: " + ex.Message, "OK");
                }
            }
        }

        private async Task UpdateFirmware(Sensor sensor)
        {
            if (sensor == null) return;

            var result = await DisplayPromptAsync(
                "Update Firmware",
                "Enter new firmware version:",
                initialValue: sensor.FirmwareVersion
            );

            if (!string.IsNullOrEmpty(result))
            {
                try
                {
                    await _sensorService.UpdateSensorFirmwareAsync(sensor.Id, result);
                    await DisplayAlert("Success", "Firmware updated", "OK");
                    LoadSensors();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to update firmware: " + ex.Message, "OK");
                }
            }
        }

        private async Task ScheduleMaintenance(Sensor sensor)
        {
            if (sensor == null) return;

            var result = await DisplayPromptAsync(
                "Schedule Maintenance",
                "Enter maintenance date (yyyy-MM-dd):",
                initialValue: DateTime.Now.AddDays(7).ToString("yyyy-MM-dd")
            );

            if (!string.IsNullOrEmpty(result) && DateTime.TryParse(result, out DateTime maintenanceDate))
            {
                try
                {
                    await _sensorService.ScheduleMaintenanceAsync(sensor.Id, maintenanceDate);
                    await DisplayAlert("Success", "Maintenance scheduled", "OK");
                    LoadSensors();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to schedule maintenance: " + ex.Message, "OK");
                }
            }
        }

        private async Task EditSensor(Sensor sensor)
        {
            if (sensor == null) return;

            // TODO: Implement edit sensor functionality
            await DisplayAlert("Info", "Edit sensor functionality coming soon", "OK");
        }

        private async Task ToggleSensor(Sensor sensor)
        {
            if (sensor == null) return;

            try
            {
                var newStatus = sensor.IsActive ? "Inactive" : "Active";
                await _sensorService.UpdateSensorStatusAsync(sensor.Id, newStatus);
                LoadSensors();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to toggle sensor: " + ex.Message, "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 