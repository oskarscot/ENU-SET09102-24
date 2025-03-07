using SET09102.Services.Administration;
using SET09102.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102.Administrator.Pages
{
    public partial class SensorConfigPage : ContentPage, INotifyPropertyChanged
    {
        private readonly ConfigService _configService;
        public ObservableCollection<SensorConfig> Sensors { get; set; }
        public SensorConfig SelectedSensor { get; set; }
        public double FirmwareProgress { get; set; }
        public bool IsUpdatingFirmware { get; set; }

        public SensorConfigPage(ConfigService configService)
        {
            InitializeComponent();
            _configService = configService;
            Sensors = new ObservableCollection<SensorConfig>();
            _configService.OnProgressUpdated += progress => { FirmwareProgress = progress / 100.0; OnPropertyChanged(nameof(FirmwareProgress)); };
            LoadSensors();
        }

        private void LoadSensors()
        {
            // Mock data for now
            Sensors.Add(new SensorConfig { SensorId = 1, PollingInterval = 10 });
            Sensors.Add(new SensorConfig { SensorId = 2, PollingInterval = 15 });
        }

        public ICommand UpdateConfigCommand => new Command(async () =>
        {
            if (SelectedSensor != null)
                await _configService.UpdateConfigAsync(SelectedSensor);
        });

        public ICommand UpdateFirmwareCommand => new Command(async () =>
        {
            if (SelectedSensor != null)
            {
                IsUpdatingFirmware = true;
                OnPropertyChanged(nameof(IsUpdatingFirmware));
                await _configService.SimulateFirmwareUpdateAsync(SelectedSensor.SensorId);
                IsUpdatingFirmware = false;
                OnPropertyChanged(nameof(IsUpdatingFirmware));
            }
        });

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}