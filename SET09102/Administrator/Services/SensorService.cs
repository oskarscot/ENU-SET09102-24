using Microsoft.Data.SqlClient;
using SET09102.Administrator.Models;

namespace SET09102.Administrator.Services
{
    /// <summary>
    /// Service responsible for managing sensor operations including configuration, firmware updates,
    /// maintenance scheduling, and status management.
    /// </summary>
    public class SensorService
    {
        private readonly string _connectionString;
        private readonly AuditService _auditService;

        /// <summary>
        /// Initializes a new instance of the SensorService class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="auditService">The audit service for logging operations.</param>
        public SensorService(string connectionString, AuditService auditService)
        {
            _connectionString = connectionString;
            _auditService = auditService;
        }

        /// <summary>
        /// Retrieves all sensors from the database.
        /// </summary>
        /// <returns>A list of all sensors in the system.</returns>
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

        /// <summary>
        /// Retrieves a specific sensor by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the sensor.</param>
        /// <returns>The sensor if found, null otherwise.</returns>
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

        /// <summary>
        /// Updates the configuration of a specific sensor.
        /// </summary>
        /// <param name="sensorId">The ID of the sensor to update.</param>
        /// <param name="configuration">The new configuration settings for the sensor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Updates the firmware version of a specific sensor.
        /// </summary>
        /// <param name="sensorId">The ID of the sensor to update.</param>
        /// <param name="newVersion">The new firmware version.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Schedules maintenance for a specific sensor.
        /// </summary>
        /// <param name="sensorId">The ID of the sensor to schedule maintenance for.</param>
        /// <param name="maintenanceDate">The date when maintenance should be performed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Retrieves all scheduled maintenance events.
        /// </summary>
        /// <returns>A list of all maintenance schedules in the system.</returns>
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

        /// <summary>
        /// Updates the operational status of a specific sensor.
        /// </summary>
        /// <param name="sensorId">The ID of the sensor to update.</param>
        /// <param name="status">The new status of the sensor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Represents a sensor in the system with its properties and current state.
    /// </summary>
    public class Sensor
    {
        /// <summary>
        /// The unique identifier of the sensor.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the sensor.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the sensor (e.g., temperature, humidity, pressure).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The physical location of the sensor.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The current operational status of the sensor.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The date of the last maintenance performed on the sensor.
        /// </summary>
        public DateTime? LastMaintenance { get; set; }

        /// <summary>
        /// The current firmware version installed on the sensor.
        /// </summary>
        public string FirmwareVersion { get; set; }

        /// <summary>
        /// The current configuration settings of the sensor.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Indicates whether the sensor is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The date when the sensor was first added to the system.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date when the sensor was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents a scheduled maintenance event for a sensor.
    /// </summary>
    public class MaintenanceSchedule
    {
        /// <summary>
        /// The unique identifier of the maintenance schedule.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the sensor that requires maintenance.
        /// </summary>
        public int SensorId { get; set; }

        /// <summary>
        /// The name of the sensor that requires maintenance.
        /// </summary>
        public string SensorName { get; set; }

        /// <summary>
        /// The date when maintenance is scheduled to be performed.
        /// </summary>
        public DateTime ScheduledDate { get; set; }

        /// <summary>
        /// The current status of the maintenance schedule.
        /// </summary>
        public string Status { get; set; }
    }
} 