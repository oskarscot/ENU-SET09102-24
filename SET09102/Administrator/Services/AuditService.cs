using Microsoft.Data.SqlClient;
using SET09102.Administrator.Models;

namespace SET09102.Administrator.Services
{
    public class AuditService
    {
        private readonly string _connectionString;

        public AuditService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task LogEventAsync(string eventType, string description, int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"INSERT INTO AuditLogs (EventType, Description, UserId, Timestamp)
                  VALUES (@EventType, @Description, @UserId, @Timestamp)", 
                connection);

            cmd.Parameters.AddWithValue("@EventType", eventType);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, int? userId = null)
        {
            var logs = new List<AuditLog>();
            using var connection = new SqlConnection(_connectionString);
            
            var query = @"SELECT Id, EventType, Description, UserId, Timestamp
                         FROM AuditLogs
                         WHERE 1=1";
            
            if (startDate.HasValue)
                query += " AND Timestamp >= @StartDate";
            if (endDate.HasValue)
                query += " AND Timestamp <= @EndDate";
            if (userId.HasValue)
                query += " AND UserId = @UserId";
            
            query += " ORDER BY Timestamp DESC";

            using var cmd = new SqlCommand(query, connection);

            if (startDate.HasValue)
                cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
            if (endDate.HasValue)
                cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
            if (userId.HasValue)
                cmd.Parameters.AddWithValue("@UserId", userId.Value);

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

            return logs;
        }

        public async Task<List<AuditLog>> GetUserAuditLogsAsync(int userId)
        {
            return await GetAuditLogsAsync(userId: userId);
        }

        public async Task<List<AuditLog>> GetSecurityAuditLogsAsync()
        {
            var logs = new List<AuditLog>();
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT Id, EventType, Description, UserId, Timestamp
                  FROM AuditLogs
                  WHERE EventType IN ('Login', 'Logout', 'PasswordChange', 'UserCreated', 'UserDeleted', 'RoleChanged')
                  ORDER BY Timestamp DESC", 
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

            return logs;
        }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public int? UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 