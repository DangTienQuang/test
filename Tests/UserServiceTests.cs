using System;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoWashPro.Tests
{
    public class UserServiceTests
    {
        private AutoWashDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AutoWashDbContext(options);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithDuplicateEmail_ThrowsException()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var dbContext = GetDbContext(dbName);

            var user1 = new User
            {
                UserId = 1,
                PhoneNumber = "0901234567",
                Email = "user1@example.com",
                PasswordHash = "hash1",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User One",
                    TierId = 1
                }
            };

            var user2 = new User
            {
                UserId = 2,
                PhoneNumber = "0909876543",
                Email = "user2@example.com",
                PasswordHash = "hash2",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User Two",
                    TierId = 1
                }
            };

            dbContext.Users.Add(user1);
            dbContext.Users.Add(user2);
            await dbContext.SaveChangesAsync();

            var service = new UserService(dbContext);
            var updateDto = new UpdateUserProfileDTO
            {
                Email = "user2@example.com" // Attempting to use user2's email
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UpdateProfileAsync(user1.UserId, updateDto));
            Assert.Equal("Email này đã được sử dụng bởi tài khoản khác.", exception.Message);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithDuplicatePhoneAndWhitespace_ThrowsException()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var dbContext = GetDbContext(dbName);

            var user1 = new User
            {
                UserId = 1,
                PhoneNumber = "0901234567",
                Email = "user1@example.com",
                PasswordHash = "hash1",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User One",
                    TierId = 1
                }
            };

            var user2 = new User
            {
                UserId = 2,
                PhoneNumber = "0909876543",
                Email = "user2@example.com",
                PasswordHash = "hash2",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User Two",
                    TierId = 1
                }
            };

            dbContext.Users.Add(user1);
            dbContext.Users.Add(user2);
            await dbContext.SaveChangesAsync();

            var service = new UserService(dbContext);
            var updateDto = new UpdateUserProfileDTO
            {
                PhoneNumber = "  0909876543  " // Attempting to use user2's phone with whitespace
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UpdateProfileAsync(user1.UserId, updateDto));
            Assert.Equal("Số điện thoại này đã được sử dụng bởi tài khoản khác.", exception.Message);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithDuplicatePhone_ThrowsException()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var dbContext = GetDbContext(dbName);

            var user1 = new User
            {
                UserId = 1,
                PhoneNumber = "0901234567",
                Email = "user1@example.com",
                PasswordHash = "hash1",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User One",
                    TierId = 1
                }
            };

            var user2 = new User
            {
                UserId = 2,
                PhoneNumber = "0909876543",
                Email = "user2@example.com",
                PasswordHash = "hash2",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User Two",
                    TierId = 1
                }
            };

            dbContext.Users.Add(user1);
            dbContext.Users.Add(user2);
            await dbContext.SaveChangesAsync();

            var service = new UserService(dbContext);
            var updateDto = new UpdateUserProfileDTO
            {
                PhoneNumber = "0909876543" // Attempting to use user2's phone
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UpdateProfileAsync(user1.UserId, updateDto));
            Assert.Equal("Số điện thoại này đã được sử dụng bởi tài khoản khác.", exception.Message);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithSamePhone_DoesNotThrow()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var dbContext = GetDbContext(dbName);

            var user1 = new User
            {
                UserId = 1,
                PhoneNumber = "0901234567",
                Email = "user1@example.com",
                PasswordHash = "hash1",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "User One",
                    TierId = 1
                }
            };

            dbContext.Users.Add(user1);
            await dbContext.SaveChangesAsync();

            var service = new UserService(dbContext);
            var updateDto = new UpdateUserProfileDTO
            {
                PhoneNumber = "  0901234567  " // Same phone number but with spaces
            };

            // Act
            var result = await service.UpdateProfileAsync(user1.UserId, updateDto);

            // Assert
            Assert.True(result); // Should succeed without modifying or throwing
            var updatedUser = await dbContext.Users.FirstAsync(u => u.UserId == user1.UserId);
            Assert.Equal("0901234567", updatedUser.PhoneNumber);
        }

        [Fact]
        public async Task UpdateProfileAsync_WithValidData_UpdatesProfileSuccessfully()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var dbContext = GetDbContext(dbName);

            var user = new User
            {
                UserId = 1,
                PhoneNumber = "0901234567",
                Email = "old@example.com",
                PasswordHash = "hash",
                Role = "Customer",
                Status = "Active",
                CustomerProfile = new CustomerProfile
                {
                    FullName = "Old Name",
                    TierId = 1
                }
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var service = new UserService(dbContext);
            var updateDto = new UpdateUserProfileDTO
            {
                FullName = "New Name",
                PhoneNumber = "0909999999",
                Email = "new@example.com"
            };

            // Act
            var result = await service.UpdateProfileAsync(user.UserId, updateDto);

            // Assert
            Assert.True(result);
            var updatedUser = await dbContext.Users.Include(u => u.CustomerProfile).FirstAsync(u => u.UserId == user.UserId);
            Assert.Equal("New Name", updatedUser.CustomerProfile.FullName);
            Assert.Equal("0909999999", updatedUser.PhoneNumber);
            Assert.Equal("new@example.com", updatedUser.Email);
        }

        [Fact]
        public async Task UpdateProfileAsync_UserNotFound_ThrowsException()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var dbContext = GetDbContext(dbName);
            var service = new UserService(dbContext);
            var updateDto = new UpdateUserProfileDTO
            {
                FullName = "New Name"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UpdateProfileAsync(999, updateDto));
            Assert.Equal("Không tìm thấy dữ liệu người dùng.", exception.Message);
        }
    }
}
