using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using SET09102.Models;
using BCrypt.Net;

namespace SET09102.Services.Administration
{
    public class AuthService
    {
        private readonly SqlConnection _connection;
        private int _failedAttempts = 0;
        private DateTime? _lockoutEnd;

        public AuthService(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_lockoutEnd.HasValue && DateTime.Now < _lockoutEnd)
                return false;

            using var cmd = new SqlCommand("SELECT PasswordHash FROM Users WHERE Username = @u", _connection);
            cmd.Parameters.AddWithValue("@u", username);
            await _connection.OpenAsync();
            var hash = (string)await cmd.ExecuteScalarAsync();
            await _connection.CloseAsync();

            if (hash != null && BCrypt.Net.BCrypt.Verify(password, hash))
            {
                _failedAttempts = 0;
                await LogAttemptAsync(username, true);
                return true;
            }

            _failedAttempts++;
            await LogAttemptAsync(username, false);
            if (_failedAttempts >= 5)
                _lockoutEnd = DateTime.Now.AddMinutes(10);
            return false;
        }

        private async Task LogAttemptAsync(string username, bool success)
        {
            using var cmd = new SqlCommand(
                "INSERT INTO LoginAttempts (Username, Timestamp, Success) VALUES (@u, @t, @s)",
                _connection);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@t", DateTime.Now);
            cmd.Parameters.AddWithValue("@s", success);
            await _connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await _connection.CloseAsync();
        }
    }
}