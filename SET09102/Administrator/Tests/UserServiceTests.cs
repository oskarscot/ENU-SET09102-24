using Xunit;
using Moq;
using Microsoft.Data.SqlClient;
using SET09102.Administrator.Services;
using SET09102.Administrator.Models;

namespace SET09102.Administrator.Tests
{
    public class UserServiceTests
    {
        private readonly string _connectionString = "Server=localhost;Database=SET09102;Trusted_Connection=True;";
        private readonly Mock<AuditService> _mockAuditService;

        public UserServiceTests()
        {
            _mockAuditService = new Mock<AuditService>();
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsUsers()
        {
            var service = new UserService(_connectionString, _mockAuditService.Object);
            var users = await service.GetAllUsersAsync();
            Assert.NotNull(users);
        }
    }
}