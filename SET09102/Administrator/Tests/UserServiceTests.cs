using Xunit;
using Moq;
using Microsoft.Data.SqlClient;
using SET09102.Services.Administration;
using SET09102.Models;

namespace SET09102.Tests
{
    public class UserServiceTests
    {
        [Fact]
        public async Task GetAllUsersAsync_ReturnsUsers()
        {
            var mockConnection = new Mock<SqlConnection>();
            var service = new UserService(mockConnection.Object);
            var users = await service.GetAllUsersAsync();
            Assert.NotNull(users);
        }
    }
}