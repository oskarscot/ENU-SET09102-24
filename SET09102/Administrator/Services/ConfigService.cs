using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using SET09102.Models;

namespace SET09102.Services.Administration
{
    public class ConfigService
    {
        private readonly SqlConnection _connection;
        public event Action<int> OnProgressUpdated;

        public ConfigService(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task UpdateConfigAsync(SensorConfig config)
        {
            using var cmd = new SqlCommand(
                "UPDATE SensorConfigs SET PollingInterval = @p WHERE SensorId = @s",
                _connection);
            cmd.Parameters.AddWithValue("@p", config.PollingInterval);
            cmd.Parameters.AddWithValue("@s", config.SensorId);
            await _connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await _connection.CloseAsync();
            await LogUpdateAsync(config.SensorId, "Config", true);
        }

        public async Task SimulateFirmwareUpdateAsync(int sensorId)
        {
            for (int i = 0; i <= 100; i += 10)
            {
                OnProgressUpdated?.Invoke(i);
                await Task.Delay(1000); // 10-second simulation
            }
            await LogUpdateAsync(sensorId, "Firmware", true);
        }

        private async Task LogUpdateAsync(int sensorId, string updateType, bool success)
        {
            using var cmd = new SqlCommand(
                "INSERT INTO UpdateLog (SensorId, UpdateType, Timestamp, Success) VALUES (@s, @t, @d, @u)",
                _connection);
            cmd.Parameters.AddWithValue("@s", sensorId);
            cmd.Parameters.AddWithValue("@t", updateType);
            cmd.Parameters.AddWithValue("@d", DateTime.Now);
            cmd.Parameters.AddWithValue("@u", success);
            await _connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await _connection.CloseAsync();
        }
    }
}