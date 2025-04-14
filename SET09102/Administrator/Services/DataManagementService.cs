using Microsoft.Data.SqlClient;
using System.IO.Compression;
using System.Text.Json;
using SET09102.Administrator.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SET09102.Administrator.Services
{
    public class DataManagementService
    {
        private readonly string _connectionString;
        private readonly AuditService _auditService;
        private readonly string _backupDirectory;
        private readonly string _exportDirectory;

        public DataManagementService(string connectionString, AuditService auditService)
        {
            _connectionString = connectionString;
            _auditService = auditService;
            _backupDirectory = Path.Combine(FileSystem.AppDataDirectory, "Backups");
            _exportDirectory = Path.Combine(FileSystem.AppDataDirectory, "Exports");
            
            Directory.CreateDirectory(_backupDirectory);
            Directory.CreateDirectory(_exportDirectory);
        }

        public async Task<string> CreateBackupAsync(string? description = null)
        {
            string backupFileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string backupPath = Path.Combine(_backupDirectory, backupFileName);
            
            // Create a temporary directory for the backup
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Export data to JSON files
                await ExportUsersToJsonAsync(Path.Combine(tempDir, "users.json"));
                await ExportSensorsToJsonAsync(Path.Combine(tempDir, "sensors.json"));
                await ExportAuditLogsToJsonAsync(Path.Combine(tempDir, "audit_logs.json"));
                await ExportMaintenanceScheduleToJsonAsync(Path.Combine(tempDir, "maintenance_schedule.json"));
                
                // Create a metadata file
                var metadata = new
                {
                    BackupDate = DateTime.UtcNow,
                    Description = description,
                    Version = "1.0"
                };
                
                File.WriteAllText(
                    Path.Combine(tempDir, "metadata.json"), 
                    JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true })
                );
                
                // Create a zip file
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                
                ZipFile.CreateFromDirectory(tempDir, backupPath);
                
                // Log the backup event
                await _auditService.LogEventAsync(
                    "DataBackup",
                    $"Created backup: {backupFileName}" + (description != null ? $" - {description}" : "")
                );
                
                return backupPath;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public async Task RestoreFromBackupAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Backup file not found", backupPath);
            }
            
            // Create a temporary directory for extraction
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Extract the backup
                ZipFile.ExtractToDirectory(backupPath, tempDir);
                
                // Verify metadata
                string metadataPath = Path.Combine(tempDir, "metadata.json");
                if (!File.Exists(metadataPath))
                {
                    throw new InvalidDataException("Backup file is missing metadata");
                }
                
                // Import data from JSON files
                await ImportUsersFromJsonAsync(Path.Combine(tempDir, "users.json"));
                await ImportSensorsFromJsonAsync(Path.Combine(tempDir, "sensors.json"));
                await ImportAuditLogsFromJsonAsync(Path.Combine(tempDir, "audit_logs.json"));
                await ImportMaintenanceScheduleFromJsonAsync(Path.Combine(tempDir, "maintenance_schedule.json"));
                
                // Log the restore event
                await _auditService.LogEventAsync(
                    "DataRestore",
                    $"Restored from backup: {Path.GetFileName(backupPath)}"
                );
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public async Task<List<string>> GetBackupFilesAsync()
        {
            return Directory.GetFiles(_backupDirectory, "backup_*.zip")
                           .Select(Path.GetFileName)
                           .OrderByDescending(f => f)
                           .ToList();
        }

        public async Task DeleteBackupAsync(string backupFileName)
        {
            string backupPath = Path.Combine(_backupDirectory, backupFileName);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                
                // Log the deletion event
                await _auditService.LogEventAsync(
                    "BackupDeleted",
                    $"Deleted backup: {backupFileName}"
                );
            }
        }

        public async Task ApplyRetentionPolicyAsync(int daysToKeep)
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, "backup_*.zip");
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            
            foreach (var file in backupFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    await DeleteBackupAsync(Path.GetFileName(file));
                }
            }
            
            // Log the retention policy application
            await _auditService.LogEventAsync(
                "RetentionPolicy",
                $"Applied retention policy: keeping backups for {daysToKeep} days"
            );
        }

        public async Task<string> ExportDataToJsonAsync(string dataType)
        {
            string fileName = $"{dataType}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(_backupDirectory, fileName);
            
            switch (dataType.ToLower())
            {
                case "users":
                    await ExportUsersToJsonAsync(filePath);
                    break;
                case "sensors":
                    await ExportSensorsToJsonAsync(filePath);
                    break;
                case "audit_logs":
                    await ExportAuditLogsToJsonAsync(filePath);
                    break;
                case "maintenance_schedule":
                    await ExportMaintenanceScheduleToJsonAsync(filePath);
                    break;
                default:
                    throw new ArgumentException($"Unknown data type: {dataType}");
            }
            
            // Log the export event
            await _auditService.LogEventAsync(
                "DataExport",
                $"Exported {dataType} to {fileName}"
            );
            
            return filePath;
        }

        public async Task ImportDataFromJsonAsync(string filePath, string dataType)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Import file not found", filePath);
            }
            
            switch (dataType.ToLower())
            {
                case "users":
                    await ImportUsersFromJsonAsync(filePath);
                    break;
                case "sensors":
                    await ImportSensorsFromJsonAsync(filePath);
                    break;
                case "audit_logs":
                    await ImportAuditLogsFromJsonAsync(filePath);
                    break;
                case "maintenance_schedule":
                    await ImportMaintenanceScheduleFromJsonAsync(filePath);
                    break;
                default:
                    throw new ArgumentException($"Unknown data type: {dataType}");
            }
            
            // Log the import event
            await _auditService.LogEventAsync(
                "DataImport",
                $"Imported {dataType} from {Path.GetFileName(filePath)}"
            );
        }

        private async Task ExportUsersToJsonAsync(string filePath)
        {
            var users = new List<User>();
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT u.Id, u.Username, u.Email, u.PasswordHash, u.IsActive, u.CreatedAt, u.LastLoginAt,
                         r.Id as RoleId, r.Name as RoleName, r.Description as RoleDescription
                  FROM Users u
                  JOIN Roles r ON u.RoleId = r.Id", 
                connection);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    IsActive = reader.GetBoolean(4),
                    CreatedAt = reader.GetDateTime(5),
                    LastLoginAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    Role = new Role
                    {
                        Id = reader.GetInt32(7),
                        Name = reader.GetString(8),
                        Description = reader.GetString(9)
                    }
                });
            }
            
            File.WriteAllText(
                filePath, 
                JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        private async Task ImportUsersFromJsonAsync(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var users = JsonSerializer.Deserialize<List<User>>(json);
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            foreach (var user in users)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO Users (Username, Email, PasswordHash, RoleId, IsActive, CreatedAt, LastLoginAt)
                      VALUES (@Username, @Email, @PasswordHash, @RoleId, @IsActive, @CreatedAt, @LastLoginAt)", 
                    connection);

                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@RoleId", user.Role.Id);
                cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
                cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                cmd.Parameters.AddWithValue("@LastLoginAt", (object)user.LastLoginAt ?? DBNull.Value);
                
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ExportSensorsToJsonAsync(string filePath)
        {
            var sensors = new List<Sensor>();
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT Id, Name, Type, Location, Status, LastMaintenance, FirmwareVersion, 
                         Configuration, IsActive, CreatedAt, LastUpdated
                  FROM Sensors", 
                connection);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sensors.Add(new Sensor
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    Location = reader.GetString(3),
                    Status = reader.GetString(4),
                    LastMaintenance = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    FirmwareVersion = reader.GetString(6),
                    Configuration = reader.GetString(7),
                    IsActive = reader.GetBoolean(8),
                    CreatedAt = reader.GetDateTime(9),
                    LastUpdated = reader.GetDateTime(10)
                });
            }
            
            File.WriteAllText(
                filePath, 
                JsonSerializer.Serialize(sensors, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        private async Task ImportSensorsFromJsonAsync(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var sensors = JsonSerializer.Deserialize<List<Sensor>>(json);
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            foreach (var sensor in sensors)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO Sensors (Name, Type, Location, Status, LastMaintenance, FirmwareVersion, 
                                          Configuration, IsActive, CreatedAt, LastUpdated)
                      VALUES (@Name, @Type, @Location, @Status, @LastMaintenance, @FirmwareVersion, 
                              @Configuration, @IsActive, @CreatedAt, @LastUpdated)", 
                    connection);

                cmd.Parameters.AddWithValue("@Name", sensor.Name);
                cmd.Parameters.AddWithValue("@Type", sensor.Type);
                cmd.Parameters.AddWithValue("@Location", sensor.Location);
                cmd.Parameters.AddWithValue("@Status", sensor.Status);
                cmd.Parameters.AddWithValue("@LastMaintenance", (object)sensor.LastMaintenance ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FirmwareVersion", sensor.FirmwareVersion);
                cmd.Parameters.AddWithValue("@Configuration", sensor.Configuration);
                cmd.Parameters.AddWithValue("@IsActive", sensor.IsActive);
                cmd.Parameters.AddWithValue("@CreatedAt", sensor.CreatedAt);
                cmd.Parameters.AddWithValue("@LastUpdated", sensor.LastUpdated);
                
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ExportAuditLogsToJsonAsync(string filePath)
        {
            var logs = new List<AuditLog>();
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT Id, EventType, Description, UserId, Timestamp
                  FROM AuditLogs", 
                connection);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new AuditLog
                {
                    Id = reader.GetInt32(0),
                    EventType = reader.GetString(1),
                    Description = reader.GetString(2),
                    UserId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Timestamp = reader.GetDateTime(4)
                });
            }
            
            File.WriteAllText(
                filePath, 
                JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        private async Task ImportAuditLogsFromJsonAsync(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var logs = JsonSerializer.Deserialize<List<AuditLog>>(json);
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            foreach (var log in logs)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO AuditLogs (EventType, Description, UserId, Timestamp)
                      VALUES (@EventType, @Description, @UserId, @Timestamp)", 
                    connection);

                cmd.Parameters.AddWithValue("@EventType", log.EventType);
                cmd.Parameters.AddWithValue("@Description", log.Description);
                cmd.Parameters.AddWithValue("@UserId", (object)log.UserId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Timestamp", log.Timestamp);
                
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ExportMaintenanceScheduleToJsonAsync(string filePath)
        {
            var schedules = new List<MaintenanceSchedule>();
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT ms.Id, ms.SensorId, s.Name as SensorName, ms.ScheduledDate, ms.Status
                  FROM MaintenanceSchedule ms
                  JOIN Sensors s ON ms.SensorId = s.Id", 
                connection);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                schedules.Add(new MaintenanceSchedule
                {
                    Id = reader.GetInt32(0),
                    SensorId = reader.GetInt32(1),
                    SensorName = reader.GetString(2),
                    ScheduledDate = reader.GetDateTime(3),
                    Status = reader.GetString(4)
                });
            }
            
            File.WriteAllText(
                filePath, 
                JsonSerializer.Serialize(schedules, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        private async Task ImportMaintenanceScheduleFromJsonAsync(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var schedules = JsonSerializer.Deserialize<List<MaintenanceSchedule>>(json);
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            foreach (var schedule in schedules)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO MaintenanceSchedule (SensorId, ScheduledDate, Status)
                      VALUES (@SensorId, @ScheduledDate, @Status)", 
                    connection);

                cmd.Parameters.AddWithValue("@SensorId", schedule.SensorId);
                cmd.Parameters.AddWithValue("@ScheduledDate", schedule.ScheduledDate);
                cmd.Parameters.AddWithValue("@Status", schedule.Status);
                
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task ImportFromExcelAsync(string excelFilePath, string dataType)
        {
            if (!File.Exists(excelFilePath))
            {
                throw new FileNotFoundException("Excel file not found", excelFilePath);
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(excelFilePath));
            
            switch (dataType.ToLower())
            {
                case "weather":
                    await ImportWeatherDataAsync(package);
                    break;
                case "water_quality":
                    await ImportWaterQualityDataAsync(package);
                    break;
                case "air_quality":
                    await ImportAirQualityDataAsync(package);
                    break;
                case "metadata":
                    await ImportMetadataAsync(package);
                    break;
                default:
                    throw new ArgumentException($"Unknown data type: {dataType}");
            }

            await _auditService.LogEventAsync(
                "ExcelImport",
                $"Imported {dataType} from Excel file: {Path.GetFileName(excelFilePath)}"
            );
        }

        private async Task ImportWeatherDataAsync(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets[0]; // First worksheet
            if (worksheet == null)
                throw new InvalidOperationException("Weather data worksheet not found in Excel file");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Create WeatherData table if it doesn't exist
            using (var cmd = new SqlCommand(
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WeatherData' AND xtype='U')
                  CREATE TABLE WeatherData (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      Timestamp DATETIME,
                      Temperature DECIMAL(5,2),
                      Humidity DECIMAL(5,2),
                      WindSpeed DECIMAL(5,2),
                      WindDirection VARCHAR(50),
                      Precipitation DECIMAL(5,2),
                      Location VARCHAR(100)
                  )", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Import data from Excel
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO WeatherData (Timestamp, Temperature, Humidity, WindSpeed, WindDirection, Precipitation, Location)
                      VALUES (@Timestamp, @Temperature, @Humidity, @WindSpeed, @WindDirection, @Precipitation, @Location)",
                    connection);

                cmd.Parameters.AddWithValue("@Timestamp", DateTime.Parse(worksheet.Cells[row, 1].Text));
                cmd.Parameters.AddWithValue("@Temperature", decimal.Parse(worksheet.Cells[row, 2].Text));
                cmd.Parameters.AddWithValue("@Humidity", decimal.Parse(worksheet.Cells[row, 3].Text));
                cmd.Parameters.AddWithValue("@WindSpeed", decimal.Parse(worksheet.Cells[row, 4].Text));
                cmd.Parameters.AddWithValue("@WindDirection", worksheet.Cells[row, 5].Text);
                cmd.Parameters.AddWithValue("@Precipitation", decimal.Parse(worksheet.Cells[row, 6].Text));
                cmd.Parameters.AddWithValue("@Location", worksheet.Cells[row, 7].Text);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ImportWaterQualityDataAsync(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null)
                throw new InvalidOperationException("Water quality data worksheet not found in Excel file");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Create WaterQualityData table if it doesn't exist
            using (var cmd = new SqlCommand(
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WaterQualityData' AND xtype='U')
                  CREATE TABLE WaterQualityData (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      Timestamp DATETIME,
                      pH DECIMAL(4,2),
                      DissolvedOxygen DECIMAL(5,2),
                      Temperature DECIMAL(5,2),
                      Turbidity DECIMAL(5,2),
                      Conductivity DECIMAL(7,2),
                      Location VARCHAR(100)
                  )", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Import data from Excel
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO WaterQualityData (Timestamp, pH, DissolvedOxygen, Temperature, Turbidity, Conductivity, Location)
                      VALUES (@Timestamp, @pH, @DissolvedOxygen, @Temperature, @Turbidity, @Conductivity, @Location)",
                    connection);

                cmd.Parameters.AddWithValue("@Timestamp", DateTime.Parse(worksheet.Cells[row, 1].Text));
                cmd.Parameters.AddWithValue("@pH", decimal.Parse(worksheet.Cells[row, 2].Text));
                cmd.Parameters.AddWithValue("@DissolvedOxygen", decimal.Parse(worksheet.Cells[row, 3].Text));
                cmd.Parameters.AddWithValue("@Temperature", decimal.Parse(worksheet.Cells[row, 4].Text));
                cmd.Parameters.AddWithValue("@Turbidity", decimal.Parse(worksheet.Cells[row, 5].Text));
                cmd.Parameters.AddWithValue("@Conductivity", decimal.Parse(worksheet.Cells[row, 6].Text));
                cmd.Parameters.AddWithValue("@Location", worksheet.Cells[row, 7].Text);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ImportAirQualityDataAsync(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null)
                throw new InvalidOperationException("Air quality data worksheet not found in Excel file");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Create AirQualityData table if it doesn't exist
            using (var cmd = new SqlCommand(
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AirQualityData' AND xtype='U')
                  CREATE TABLE AirQualityData (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      Timestamp DATETIME,
                      PM25 DECIMAL(5,2),
                      PM10 DECIMAL(5,2),
                      NO2 DECIMAL(5,2),
                      O3 DECIMAL(5,2),
                      SO2 DECIMAL(5,2),
                      CO DECIMAL(5,2),
                      Location VARCHAR(100)
                  )", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Import data from Excel
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO AirQualityData (Timestamp, PM25, PM10, NO2, O3, SO2, CO, Location)
                      VALUES (@Timestamp, @PM25, @PM10, @NO2, @O3, @SO2, @CO, @Location)",
                    connection);

                cmd.Parameters.AddWithValue("@Timestamp", DateTime.Parse(worksheet.Cells[row, 1].Text));
                cmd.Parameters.AddWithValue("@PM25", decimal.Parse(worksheet.Cells[row, 2].Text));
                cmd.Parameters.AddWithValue("@PM10", decimal.Parse(worksheet.Cells[row, 3].Text));
                cmd.Parameters.AddWithValue("@NO2", decimal.Parse(worksheet.Cells[row, 4].Text));
                cmd.Parameters.AddWithValue("@O3", decimal.Parse(worksheet.Cells[row, 5].Text));
                cmd.Parameters.AddWithValue("@SO2", decimal.Parse(worksheet.Cells[row, 6].Text));
                cmd.Parameters.AddWithValue("@CO", decimal.Parse(worksheet.Cells[row, 7].Text));
                cmd.Parameters.AddWithValue("@Location", worksheet.Cells[row, 8].Text);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ImportMetadataAsync(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null)
                throw new InvalidOperationException("Metadata worksheet not found in Excel file");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Create Metadata table if it doesn't exist
            using (var cmd = new SqlCommand(
                @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Metadata' AND xtype='U')
                  CREATE TABLE Metadata (
                      Id INT IDENTITY(1,1) PRIMARY KEY,
                      SensorId VARCHAR(50),
                      Location VARCHAR(100),
                      Latitude DECIMAL(9,6),
                      Longitude DECIMAL(9,6),
                      InstallationDate DATETIME,
                      LastMaintenanceDate DATETIME,
                      SensorType VARCHAR(50),
                      Manufacturer VARCHAR(100),
                      Model VARCHAR(100)
                  )", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Import data from Excel
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                using var cmd = new SqlCommand(
                    @"INSERT INTO Metadata (SensorId, Location, Latitude, Longitude, InstallationDate, LastMaintenanceDate, SensorType, Manufacturer, Model)
                      VALUES (@SensorId, @Location, @Latitude, @Longitude, @InstallationDate, @LastMaintenanceDate, @SensorType, @Manufacturer, @Model)",
                    connection);

                cmd.Parameters.AddWithValue("@SensorId", worksheet.Cells[row, 1].Text);
                cmd.Parameters.AddWithValue("@Location", worksheet.Cells[row, 2].Text);
                cmd.Parameters.AddWithValue("@Latitude", decimal.Parse(worksheet.Cells[row, 3].Text));
                cmd.Parameters.AddWithValue("@Longitude", decimal.Parse(worksheet.Cells[row, 4].Text));
                cmd.Parameters.AddWithValue("@InstallationDate", DateTime.Parse(worksheet.Cells[row, 5].Text));
                cmd.Parameters.AddWithValue("@LastMaintenanceDate", DateTime.Parse(worksheet.Cells[row, 6].Text));
                cmd.Parameters.AddWithValue("@SensorType", worksheet.Cells[row, 7].Text);
                cmd.Parameters.AddWithValue("@Manufacturer", worksheet.Cells[row, 8].Text);
                cmd.Parameters.AddWithValue("@Model", worksheet.Cells[row, 9].Text);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
} 