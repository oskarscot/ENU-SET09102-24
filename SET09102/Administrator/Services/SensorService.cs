using Microsoft.Data.SqlClient;
using SET09102.Administrator.Models;

namespace SET09102.Administrator.Services
{
    public class SensorService
    {
        private readonly string _connectionString;
        private readonly AuditService _auditService;

        public SensorService(string connectionString, AuditService auditService)
        {
            _connectionString = connectionString;
            _auditService = auditService;
        }

        public async Task<List<Sensor>> GetAllSensorsAsync()
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
            return sensors;
        }

        public async Task<Sensor> GetSensorByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT Id, Name, Type, Location, Status, LastMaintenance, FirmwareVersion, 
                         Configuration, IsActive, CreatedAt, LastUpdated
                  FROM Sensors
                  WHERE Id = @Id", 
                connection);
            cmd.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Sensor
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
                };
            }
            return null;
        }

        public async Task UpdateSensorConfigurationAsync(int sensorId, string configuration)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"UPDATE Sensors 
                  SET Configuration = @Configuration,
                      LastUpdated = @LastUpdated
                  WHERE Id = @Id", 
                connection);

            cmd.Parameters.AddWithValue("@Id", sensorId);
            cmd.Parameters.AddWithValue("@Configuration", configuration);
            cmd.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync(
                "SensorConfig",
                $"Updated configuration for sensor {sensorId}"
            );
        }

        public async Task UpdateSensorFirmwareAsync(int sensorId, string newVersion)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"UPDATE Sensors 
                  SET FirmwareVersion = @FirmwareVersion,
                      LastUpdated = @LastUpdated
                  WHERE Id = @Id", 
                connection);

            cmd.Parameters.AddWithValue("@Id", sensorId);
            cmd.Parameters.AddWithValue("@FirmwareVersion", newVersion);
            cmd.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync(
                "FirmwareUpdate",
                $"Updated firmware to version {newVersion} for sensor {sensorId}"
            );
        }

        public async Task ScheduleMaintenanceAsync(int sensorId, DateTime maintenanceDate)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"INSERT INTO MaintenanceSchedule (SensorId, ScheduledDate, Status)
                  VALUES (@SensorId, @ScheduledDate, 'Scheduled')", 
                connection);

            cmd.Parameters.AddWithValue("@SensorId", sensorId);
            cmd.Parameters.AddWithValue("@ScheduledDate", maintenanceDate);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync(
                "MaintenanceScheduled",
                $"Scheduled maintenance for sensor {sensorId} on {maintenanceDate:yyyy-MM-dd}"
            );
        }

        public async Task<List<MaintenanceSchedule>> GetMaintenanceScheduleAsync()
        {
            var schedules = new List<MaintenanceSchedule>();
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT ms.Id, ms.SensorId, s.Name as SensorName, ms.ScheduledDate, ms.Status
                  FROM MaintenanceSchedule ms
                  JOIN Sensors s ON ms.SensorId = s.Id
                  ORDER BY ms.ScheduledDate", 
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
            return schedules;
        }

        public async Task UpdateSensorStatusAsync(int sensorId, string status)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"UPDATE Sensors 
                  SET Status = @Status,
                      LastUpdated = @LastUpdated
                  WHERE Id = @Id", 
                connection);

            cmd.Parameters.AddWithValue("@Id", sensorId);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync(
                "SensorStatus",
                $"Updated status to {status} for sensor {sensorId}"
            );
        }
    }

    public class Sensor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public DateTime? LastMaintenance { get; set; }
        public string FirmwareVersion { get; set; }
        public string Configuration { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class MaintenanceSchedule
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public string SensorName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; }
    }
} 