using System;
using System.Threading.Tasks;
using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using AutoWashPro.BLL.Constants;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoWashPro.Tests
{
    public class UserServiceTests : IDisposable
    {
        private readonly AutoWashDbContext _context;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AutoWashDbContext(options);
            _userService = new UserService(_context);
        }

        [Fact]
        public async Task GetCustomerDetailByAdminAsync_WithAdminRole_ThrowsException()
        {
            // Arrange
            var adminUser = new User
            {
                UserId = 1,
                PhoneNumber = "1234567890",
                PasswordHash = "hash",
                Role = UserRoles.Admin,
                Status = UserStatuses.Active,
                CustomerProfile = new CustomerProfile { FullName = "Admin User" }
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.GetCustomerDetailByAdminAsync(1));
            Assert.Equal("Không tìm thấy khách hàng này.", exception.Message);
        }

        [Fact]
        public async Task GetCustomerDetailByAdminAsync_WithNonExistentUser_ThrowsException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _userService.GetCustomerDetailByAdminAsync(999));
            Assert.Equal("Không tìm thấy khách hàng này.", exception.Message);
        }

        [Fact]
        public async Task GetCustomerDetailByAdminAsync_WithCustomerRole_ReturnsProfile()
        {
            // Arrange
            var customerUser = new User
            {
                UserId = 2,
                PhoneNumber = "0987654321",
                PasswordHash = "hash",
                Role = UserRoles.Customer,
                Status = UserStatuses.Active,
                CustomerProfile = new CustomerProfile { FullName = "Customer User", TotalPoint = 100 }
            };

            _context.Users.Add(customerUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetCustomerDetailByAdminAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.UserId);
            Assert.Equal("Customer User", result.FullName);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
