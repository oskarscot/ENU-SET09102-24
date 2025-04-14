using SET09102.Administrator.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SET09102.Administrator.Pages;

public partial class DataManagementPage : ContentPage
{
    private readonly DataManagementService _dataManagementService;
    private ObservableCollection<string> _backupFiles;
    private string _backupDescription;
    private string _selectedDataType;
    private List<string> _dataTypes;

    public ObservableCollection<string> BackupFiles
    {
        get => _backupFiles;
        set
        {
            _backupFiles = value;
            OnPropertyChanged();
        }
    }

    public string BackupDescription
    {
        get => _backupDescription;
        set
        {
            _backupDescription = value;
            OnPropertyChanged();
        }
    }

    public string SelectedDataType
    {
        get => _selectedDataType;
        set
        {
            _selectedDataType = value;
            OnPropertyChanged();
        }
    }

    public List<string> DataTypes
    {
        get => _dataTypes;
        set
        {
            _dataTypes = value;
            OnPropertyChanged();
        }
    }

    public ICommand CreateBackupCommand { get; }
    public ICommand RestoreBackupCommand { get; }
    public ICommand DeleteBackupCommand { get; }
    public ICommand ApplyRetentionPolicyCommand { get; }
    public ICommand ExportDataCommand { get; }
    public ICommand ImportDataCommand { get; }
    public ICommand ImportExcelCommand { get; }

    public DataManagementPage(DataManagementService dataManagementService)
    {
        InitializeComponent();
        _dataManagementService = dataManagementService;
        BindingContext = this;

        _dataTypes = new List<string>
        {
            "Weather",
            "Water_Quality",
            "Air_Quality",
            "Metadata"
        };

        CreateBackupCommand = new Command(async () => await CreateBackup());
        RestoreBackupCommand = new Command<string>(async (backupFile) => await RestoreBackup(backupFile));
        DeleteBackupCommand = new Command<string>(async (backupFile) => await DeleteBackup(backupFile));
        ApplyRetentionPolicyCommand = new Command(async () => await ApplyRetentionPolicy());
        ExportDataCommand = new Command(async () => await ExportData());
        ImportDataCommand = new Command(async () => await ImportData());
        ImportExcelCommand = new Command(async () => await ImportExcel());

        LoadBackupFiles();
    }

    private async void LoadBackupFiles()
    {
        try
        {
            var files = await _dataManagementService.GetBackupFilesAsync();
            BackupFiles = new ObservableCollection<string>(files);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load backup files: {ex.Message}", "OK");
        }
    }

    private async Task CreateBackup()
    {
        try
        {
            await _dataManagementService.CreateBackupAsync(BackupDescription);
            BackupDescription = string.Empty;
            LoadBackupFiles();
            await DisplayAlert("Success", "Backup created successfully", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to create backup: {ex.Message}", "OK");
        }
    }

    private async Task RestoreBackup(string backupFile)
    {
        try
        {
            var result = await DisplayAlert("Confirm Restore",
                "Are you sure you want to restore this backup? This will overwrite current data.",
                "Yes", "No");

            if (result)
            {
                await _dataManagementService.RestoreFromBackupAsync(backupFile);
                await DisplayAlert("Success", "Backup restored successfully", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to restore backup: {ex.Message}", "OK");
        }
    }

    private async Task DeleteBackup(string backupFile)
    {
        try
        {
            var result = await DisplayAlert("Confirm Delete",
                "Are you sure you want to delete this backup?",
                "Yes", "No");

            if (result)
            {
                await _dataManagementService.DeleteBackupAsync(backupFile);
                LoadBackupFiles();
                await DisplayAlert("Success", "Backup deleted successfully", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete backup: {ex.Message}", "OK");
        }
    }

    private async Task ApplyRetentionPolicy()
    {
        try
        {
            var days = await DisplayPromptAsync("Retention Policy",
                "Enter number of days to retain backups:",
                "OK", "Cancel",
                "30", -1, Keyboard.Numeric);

            if (int.TryParse(days, out int retentionDays))
            {
                await _dataManagementService.ApplyRetentionPolicyAsync(retentionDays);
                LoadBackupFiles();
                await DisplayAlert("Success", "Retention policy applied successfully", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to apply retention policy: {ex.Message}", "OK");
        }
    }

    private async Task ExportData()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedDataType))
            {
                await DisplayAlert("Error", "Please select a data type to export", "OK");
                return;
            }

            var filePath = await _dataManagementService.ExportDataToJsonAsync(SelectedDataType);
            await DisplayAlert("Success", $"Data exported successfully to: {filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to export data: {ex.Message}", "OK");
        }
    }

    private async Task ImportData()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedDataType))
            {
                await DisplayAlert("Error", "Please select a data type to import", "OK");
                return;
            }

            var result = await FilePicker.PickAsync();
            if (result != null)
            {
                await _dataManagementService.ImportDataFromJsonAsync(result.FullPath, SelectedDataType);
                await DisplayAlert("Success", "Data imported successfully", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to import data: {ex.Message}", "OK");
        }
    }

    private async Task ImportExcel()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedDataType))
            {
                await DisplayAlert("Error", "Please select a data type to import", "OK");
                return;
            }

            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.spreadsheet" } },
                        { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                        { DevicePlatform.WinUI, new[] { ".xlsx" } },
                        { DevicePlatform.macOS, new[] { "xlsx" } }
                    })
            });

            if (result != null)
            {
                var confirmResult = await DisplayAlert(
                    "Confirm Import",
                    $"Are you sure you want to import {SelectedDataType} data from {result.FileName}? This may overwrite existing data.",
                    "Yes", "No");

                if (confirmResult)
                {
                    await _dataManagementService.ImportFromExcelAsync(result.FullPath, SelectedDataType);
                    await DisplayAlert("Success", "Data imported successfully from Excel", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to import Excel data: {ex.Message}", "OK");
        }
    }
} 