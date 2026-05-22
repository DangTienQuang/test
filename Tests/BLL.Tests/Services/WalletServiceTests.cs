using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PayOS;
using Xunit;

namespace AutoWashPro.BLL.Tests.Services
{
    public class WalletServiceTests
    {
        private readonly Mock<ILogger<WalletService>> _loggerMock;
        private readonly PayOSClient _payOSClient;

        public WalletServiceTests()
        {
            _loggerMock = new Mock<ILogger<WalletService>>();
            _payOSClient = new PayOSClient("clientId", "apiKey", "checksumKey");
        }

        [Fact]
        public async Task AwardCompletionPointsAsync_UserNotFound_ThrowsException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new AutoWashDbContext(options))
            {
                var service = new WalletService(context, _payOSClient, _loggerMock.Object);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<Exception>(() =>
                    service.AwardCompletionPointsAsync(userId: 1, pointsEarned: 10, bookingId: 100));

                Assert.Equal("Không tìm thấy hồ sơ khách hàng.", exception.Message);
            }
        }

        [Fact]
        public async Task AwardCompletionPointsAsync_PointsEarnedZero_ReturnsZero()
        {
             // Arrange
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new AutoWashDbContext(options))
            {
                var service = new WalletService(context, _payOSClient, _loggerMock.Object);

                // Act
                var result = await service.AwardCompletionPointsAsync(userId: 1, pointsEarned: 0, bookingId: 100);

                // Assert
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public async Task AwardCompletionPointsAsync_ValidRequest_UpdatesProfileAndAddsLedger()
        {
             // Arrange
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var userId = 1;
            var initialTotalPoints = 50;
            var initialPromotionPoints = 10;
            var pointsEarned = 25;
            var bookingId = 100;

            using (var context = new AutoWashDbContext(options))
            {
                context.Users.Add(new User { UserId = userId, PhoneNumber = "1234567890", PasswordHash = "hash", Role = "Customer", Status = "Active", Email = "test@test.com" });
                context.CustomerProfiles.Add(new CustomerProfile
                {
                    UserId = userId,
                    FullName = "Test User",
                    TotalPoint = initialTotalPoints,
                    PromotionPoint = initialPromotionPoints
                });
                await context.SaveChangesAsync();
            }

            using (var context = new AutoWashDbContext(options))
            {
                var service = new WalletService(context, _payOSClient, _loggerMock.Object);

                // Act
                var result = await service.AwardCompletionPointsAsync(userId: userId, pointsEarned: pointsEarned, bookingId: bookingId);

                // Assert
                Assert.Equal(pointsEarned, result);

                var profile = await context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
                Assert.NotNull(profile);
                Assert.Equal(initialTotalPoints + pointsEarned, profile.TotalPoint);
                Assert.Equal(initialPromotionPoints + pointsEarned, profile.PromotionPoint);

                var ledgerEntry = await context.PointLedgers.FirstOrDefaultAsync(pl => pl.UserId == userId);
                Assert.NotNull(ledgerEntry);
                Assert.Equal(pointsEarned, ledgerEntry.PointsAdded);
                Assert.Equal($"{PointConstants.CompletionReasonPrefix} #{bookingId}", ledgerEntry.Reason);
                Assert.True(ledgerEntry.ExpiryDate > DateTime.UtcNow);
                Assert.True(ledgerEntry.TransactionDate <= DateTime.UtcNow);
            }
        }
    }
}
