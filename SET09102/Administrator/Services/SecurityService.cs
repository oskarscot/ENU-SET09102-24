using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using SET09102.Administrator.Models;

namespace SET09102.Administrator.Services
{
    public class SecurityService
    {
        private readonly string _connectionString;
        private const int SaltSize = 16;
        private const int HashSize = 20;
        private const int Iterations = 10000;

        public SecurityService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = GetHash(password, salt);
            byte[] hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            byte[] hash = GetHash(password, salt);
            for (int i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                    return false;
            }
            return true;
        }

        private byte[] GetHash(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        public async Task<bool> ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (password.Length < 8)
                return false;

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        public async Task<Session> CreateSessionAsync(User user)
        {
            var session = new Session
            {
                UserId = user.Id,
                Token = GenerateSessionToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"INSERT INTO Sessions (UserId, Token, CreatedAt, ExpiresAt)
                  VALUES (@UserId, @Token, @CreatedAt, @ExpiresAt)", 
                connection);

            cmd.Parameters.AddWithValue("@UserId", session.UserId);
            cmd.Parameters.AddWithValue("@Token", session.Token);
            cmd.Parameters.AddWithValue("@CreatedAt", session.CreatedAt);
            cmd.Parameters.AddWithValue("@ExpiresAt", session.ExpiresAt);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return session;
        }

        public async Task<Session> ValidateSessionAsync(string token)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                @"SELECT UserId, CreatedAt, ExpiresAt
                  FROM Sessions
                  WHERE Token = @Token AND ExpiresAt > @Now", 
                connection);

            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Session
                {
                    UserId = reader.GetInt32(0),
                    Token = token,
                    CreatedAt = reader.GetDateTime(1),
                    ExpiresAt = reader.GetDateTime(2)
                };
            }

            return null;
        }

        public async Task InvalidateSessionAsync(string token)
        {
            using var connection = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "DELETE FROM Sessions WHERE Token = @Token", 
                connection);

            cmd.Parameters.AddWithValue("@Token", token);

            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private string GenerateSessionToken()
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }
    }

    public class Session
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
} 