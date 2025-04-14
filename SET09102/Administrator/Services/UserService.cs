using Microsoft.Data.SqlClient;
using SET09102.Administrator.Models;
using System.Data;

namespace SET09102.Administrator.Services
{
    public class UserService
    {
        private readonly string _connectionString;
        private readonly AuditService _auditService;

        public UserService(string connectionString, AuditService auditService)
        {
            _connectionString = connectionString;
            _auditService = auditService;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                @"SELECT u.Id, u.Username, u.Email, u.PasswordHash, u.IsActive, u.CreatedAt, u.LastLoginAt,
                         r.Id as RoleId, r.Name as RoleName, r.Description as RoleDescription
                  FROM Users u
                  JOIN Roles r ON u.RoleId = r.Id
                  WHERE u.Username = @Username",
                connection);

            command.Parameters.AddWithValue("@Username", username);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
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
                };
            }

            return null;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                "UPDATE Users SET LastLoginAt = @LastLoginAt WHERE Id = @UserId",
                connection);

            command.Parameters.AddWithValue("@LastLoginAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync("UserLogin", $"User ID {userId} logged in");
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                @"SELECT u.Id, u.Username, u.Email, u.PasswordHash, u.IsActive, u.CreatedAt, u.LastLoginAt,
                         r.Id as RoleId, r.Name as RoleName, r.Description as RoleDescription
                  FROM Users u
                  JOIN Roles r ON u.RoleId = r.Id",
                connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

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

            return users;
        }

        public async Task CreateUserAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                @"INSERT INTO Users (Username, Email, PasswordHash, RoleId, IsActive, CreatedAt)
                  VALUES (@Username, @Email, @PasswordHash, @RoleId, @IsActive, @CreatedAt);
                  SELECT SCOPE_IDENTITY();",
                connection);

            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@RoleId", user.Role.Id);
            command.Parameters.AddWithValue("@IsActive", user.IsActive);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await connection.OpenAsync();
            user.Id = Convert.ToInt32(await command.ExecuteScalarAsync());

            await _auditService.LogEventAsync("UserCreated", $"Created user: {user.Username}");
        }

        public async Task UpdateUserAsync(User user)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                @"UPDATE Users 
                  SET Email = @Email,
                      RoleId = @RoleId,
                      IsActive = @IsActive
                  WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@RoleId", user.Role.Id);
            command.Parameters.AddWithValue("@IsActive", user.IsActive);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync("UserUpdated", $"Updated user: {user.Username}");
        }

        public async Task DeleteUserAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                "DELETE FROM Users WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", userId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            await _auditService.LogEventAsync("UserDeleted", $"Deleted user ID: {userId}");
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            var roles = new List<Role>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                "SELECT Id, Name, Description FROM Roles",
                connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                roles.Add(new Role
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2)
                });
            }

            return roles;
        }
    }
}