using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102.Models;

namespace SET09102.Services.Administration
{
    public class UserService
    {
        private readonly SqlConnection _connection;

        public UserService(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var cmd = new SqlCommand("SELECT Id, Username, RoleId FROM Users", _connection);
            await _connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    RoleId = reader.GetInt32(2)
                });
            }
            await _connection.CloseAsync();
            return users;
        }

        public async Task AddUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentException("Username cannot be empty.");

            using var cmd = new SqlCommand(
                "INSERT INTO Users (Username, PasswordHash, RoleId) VALUES (@u, @p, @r)", 
                _connection);
            cmd.Parameters.AddWithValue("@u", user.Username);
            cmd.Parameters.AddWithValue("@p", user.PasswordHash ?? "tempHash");
            cmd.Parameters.AddWithValue("@r", user.RoleId);
            await _connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await _connection.CloseAsync();
        }
    }
}