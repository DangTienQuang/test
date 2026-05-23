using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoWashPro.BLL.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IWalletService> _walletServiceMock;
        private readonly Mock<ITierService> _tierServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILogger<BookingService>> _loggerMock;

        public BookingServiceTests()
        {
            _walletServiceMock = new Mock<IWalletService>();
            _tierServiceMock = new Mock<ITierService>();
            _emailServiceMock = new Mock<IEmailService>();
            _loggerMock = new Mock<ILogger<BookingService>>();
        }

        private AutoWashDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AutoWashDbContext(options);
        }

        [Fact]
        public async Task CancelBookingAsync_LessThan4Hours_StatusCancelled_NoRefund()
        {
            // Arrange
            var context = GetDbContext();
            var service = new BookingService(context, _walletServiceMock.Object, _tierServiceMock.Object, _emailServiceMock.Object, _loggerMock.Object);

            var userId = 1;
            var bookingId = 1;

            var user = new User { UserId = userId, PhoneNumber = "123", PasswordHash = "hash", Role = "Customer", Status = "Active" };
            context.Users.Add(user);

            var wallet = new Wallet { WalletId = 1, UserId = userId, Balance = 100 };
            context.Wallets.Add(wallet);

            var booking = new Booking
            {
                BookingId = bookingId,
                UserId = userId,
                Status = "Pending",
                ScheduledTime = DateTime.UtcNow.AddHours(3.9), // Less than 4 hours
                FinalAmount = 50,
                PointsUsed = 0,
                ServiceId = 1,
                LicensePlate = "30A-12345",
                CreatedAt = DateTime.UtcNow
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CancelBookingAsync(userId, bookingId);

            // Assert
            Assert.True(result);

            var updatedBooking = await context.Bookings.FindAsync(bookingId);
            Assert.NotNull(updatedBooking);
            Assert.Equal("Cancelled", updatedBooking.Status);

            var updatedWallet = await context.Wallets.FindAsync(1);
            Assert.NotNull(updatedWallet);
            Assert.Equal(100, updatedWallet.Balance); // Balance should not change

            var transactionCount = await context.Transactions.CountAsync(t => t.WalletId == 1 && t.TransactionType == "Refund");
            Assert.Equal(0, transactionCount); // No refund transaction should be added

            _walletServiceMock.Verify(x => x.RefundSpendablePointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CancelBookingAsync_MoreThan4Hours_StatusCancelled_Refunded()
        {
            // Arrange
            var context = GetDbContext();
            var service = new BookingService(context, _walletServiceMock.Object, _tierServiceMock.Object, _emailServiceMock.Object, _loggerMock.Object);

            var userId = 2;
            var bookingId = 2;
            var voucherId = 1;
            var pointsUsed = 10;

            var user = new User { UserId = userId, PhoneNumber = "123", PasswordHash = "hash", Role = "Customer", Status = "Active" };
            context.Users.Add(user);

            var wallet = new Wallet { WalletId = 2, UserId = userId, Balance = 100 };
            context.Wallets.Add(wallet);

            var userVoucher = new UserVoucher
            {
                Id = 1,
                UserId = userId,
                VoucherId = voucherId,
                IsUsed = true,
                UsedDate = DateTime.UtcNow.AddHours(-1)
            };
            context.UserVouchers.Add(userVoucher);

            var booking = new Booking
            {
                BookingId = bookingId,
                UserId = userId,
                Status = "Pending",
                ScheduledTime = DateTime.UtcNow.AddHours(4.1), // More than 4 hours
                FinalAmount = 50,
                PointsUsed = pointsUsed,
                ServiceId = 1,
                LicensePlate = "30A-12345",
                CreatedAt = DateTime.UtcNow,
                AppliedVoucherId = voucherId
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CancelBookingAsync(userId, bookingId);

            // Assert
            Assert.True(result);

            var updatedBooking = await context.Bookings.FindAsync(bookingId);
            Assert.NotNull(updatedBooking);
            Assert.Equal("Cancelled", updatedBooking.Status);

            var updatedWallet = await context.Wallets.FindAsync(2);
            Assert.NotNull(updatedWallet);
            Assert.Equal(150, updatedWallet.Balance); // Balance should increase by FinalAmount

            var transactionCount = await context.Transactions.CountAsync(t => t.WalletId == 2 && t.TransactionType == "Refund" && t.Amount == 50);
            Assert.Equal(1, transactionCount); // Refund transaction should be added

            _walletServiceMock.Verify(x => x.RefundSpendablePointsAsync(userId, pointsUsed, $"{PointConstants.RefundPointsReasonPrefix} #{bookingId}", bookingId), Times.Once);

            var updatedUserVoucher = await context.UserVouchers.FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
            Assert.NotNull(updatedUserVoucher);
            Assert.False(updatedUserVoucher.IsUsed);
            Assert.Null(updatedUserVoucher.UsedDate);
        }

        [Fact]
        public async Task CancelBookingAsync_BookingNotFound_ThrowsException()
        {
            // Arrange
            var context = GetDbContext();
            var service = new BookingService(context, _walletServiceMock.Object, _tierServiceMock.Object, _emailServiceMock.Object, _loggerMock.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelBookingAsync(1, 1));
            Assert.Equal("Không tìm thấy lịch hẹn.", ex.Message);
        }

        [Fact]
        public async Task CancelBookingAsync_StatusNotPending_ThrowsException()
        {
            // Arrange
            var context = GetDbContext();
            var service = new BookingService(context, _walletServiceMock.Object, _tierServiceMock.Object, _emailServiceMock.Object, _loggerMock.Object);

            var userId = 1;
            var bookingId = 1;

            var booking = new Booking
            {
                BookingId = bookingId,
                UserId = userId,
                Status = "Confirmed",
                ScheduledTime = DateTime.UtcNow.AddHours(5),
                FinalAmount = 50,
                ServiceId = 1,
                LicensePlate = "30A-12345",
                CreatedAt = DateTime.UtcNow
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelBookingAsync(userId, bookingId));
            Assert.Equal("Chỉ có thể hủy lịch ở trạng thái đang chờ (Pending).", ex.Message);
        }
    }
}
