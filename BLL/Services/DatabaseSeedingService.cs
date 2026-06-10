using System;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.BLL.Services
{
    public class DatabaseSeedingService : IDatabaseSeedingService
    {
        private readonly AutoWashDbContext _context;

        public DatabaseSeedingService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAndSeedAsync()
        {
            await _context.Database.MigrateAsync();

            var now = DateTime.UtcNow;

            var firstTier = await _context.Tiers.FirstOrDefaultAsync(t => t.MinAccumulatedPoints == 0);
            if (firstTier == null)
            {
                firstTier = new AutoWashPro.DAL.Entities.Tier
                {
                    TierName = "Standard",
                    PointMultiplier = 1.0,
                    BookingWindowDays = 7,
                    MinAccumulatedPoints = 0
                };
                _context.Tiers.Add(firstTier);
                await _context.SaveChangesAsync();
            }

            var defaultBranch = await _context.Branches.FirstOrDefaultAsync();
            if (defaultBranch == null)
            {
                defaultBranch = new AutoWashPro.DAL.Entities.Branch
                {
                    Name = "Chi nhánh Trung Tâm",
                    Address = "123 Lê Lợi, Quận 1, TP.HCM",
                    IsActive = true
                };
                _context.Branches.Add(defaultBranch);
                await _context.SaveChangesAsync();

                _context.Lanes.Add(new AutoWashPro.DAL.Entities.Lane
                {
                    BranchId = defaultBranch.BranchId,
                    Name = "Làn 1 - VIP",
                    IsActive = true
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                var admin = new AutoWashPro.DAL.Entities.User
                {
                    PhoneNumber = "0999999999",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "Admin",
                    Status = "Active"
                };
                _context.Users.Add(admin);
                await _context.SaveChangesAsync();

                _context.CustomerProfiles.Add(new AutoWashPro.DAL.Entities.CustomerProfile
                {
                    UserId = admin.UserId,
                    FullName = "System Admin",
                    TierId = firstTier.TierId,
                    ChurnScore = 0,
                    TotalPoint = 0,
                    PromotionPoint = 0
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Users.AnyAsync(u => u.Role == "Manager"))
            {
                var manager = new AutoWashPro.DAL.Entities.User
                {
                    PhoneNumber = "0888888888",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                    Role = "Manager",
                    Status = "Active"
                };
                _context.Users.Add(manager);
                await _context.SaveChangesAsync();

                _context.EmployeeProfiles.Add(new AutoWashPro.DAL.Entities.EmployeeProfile
                {
                    EmployeeId = manager.UserId,
                    BranchId = defaultBranch.BranchId,
                    FullName = "Quản lý Nguyễn Văn A"
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Users.AnyAsync(u => u.Role == "Staff"))
            {
                var staff = new AutoWashPro.DAL.Entities.User
                {
                    PhoneNumber = "0777777777",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    Role = "Staff",
                    Status = "Active"
                };
                _context.Users.Add(staff);
                await _context.SaveChangesAsync();

                _context.EmployeeProfiles.Add(new AutoWashPro.DAL.Entities.EmployeeProfile
                {
                    EmployeeId = staff.UserId,
                    BranchId = defaultBranch.BranchId,
                    FullName = "Nhân viên Trần Văn B"
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Users.AnyAsync(u => u.Role == "Customer" && u.PhoneNumber == "0666666666"))
            {
                var customer = new AutoWashPro.DAL.Entities.User
                {
                    PhoneNumber = "0666666666",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    Role = "Customer",
                    Status = "Active"
                };
                _context.Users.Add(customer);
                await _context.SaveChangesAsync();

                _context.CustomerProfiles.Add(new AutoWashPro.DAL.Entities.CustomerProfile
                {
                    UserId = customer.UserId,
                    FullName = "Khách Hàng VIP",
                    TierId = firstTier.TierId,
                    ChurnScore = 0,
                    TotalPoint = 0,
                    PromotionPoint = 0
                });
                await _context.SaveChangesAsync();

                var wallet = new AutoWashPro.DAL.Entities.Wallet
                {
                    UserId = customer.UserId,
                    Balance = 1000000
                };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();

                _context.Transactions.Add(new AutoWashPro.DAL.Entities.Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = 1000000,
                    TransactionType = "Topup",
                    Description = "Hệ thống tặng tiền trải nghiệm",
                    Status = "Completed"
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Vouchers.AnyAsync())
            {
                var vouchers = new[]
                {
                    new AutoWashPro.DAL.Entities.Voucher
                    {
                        Code = "WELCOME10",
                        DiscountAmount = 10000,
                        MaxUsages = 1000,
                        MaxUsagePerUser = 1,
                        ExpiryDate = DateTime.UtcNow.AddMonths(1),
                        StartDate = DateTime.UtcNow,
                        IsActive = true,
                        PointsRequired = 0,
                        VoucherType = AutoWashPro.DAL.Enums.VoucherType.Discount,
                        CampaignType = AutoWashPro.DAL.Enums.VoucherCampaignType.Welcome,
                        MinOrderAmount = 50000,
                        RequiredTierId = firstTier.TierId
                    },
                    new AutoWashPro.DAL.Entities.Voucher
                    {
                        Code = "HAPPYHOUR",
                        DiscountAmount = 20000,
                        MaxUsages = 500,
                        MaxUsagePerUser = 2,
                        ExpiryDate = DateTime.UtcNow.AddMonths(2),
                        StartDate = DateTime.UtcNow,
                        IsActive = true,
                        PointsRequired = 50,
                        VoucherType = AutoWashPro.DAL.Enums.VoucherType.Discount,
                        CampaignType = AutoWashPro.DAL.Enums.VoucherCampaignType.Manual,
                        ValidStartTime = new TimeSpan(14, 0, 0), // 2 PM
                        ValidEndTime = new TimeSpan(16, 0, 0),   // 4 PM
                        MinOrderAmount = 100000,
                        RequiredTierId = firstTier.TierId
                    },
                    new AutoWashPro.DAL.Entities.Voucher
                    {
                        Code = "FREECOFFEE",
                        DiscountAmount = 0, // Physical gift doesn't have financial discount
                        MaxUsages = 100,
                        MaxUsagePerUser = 1,
                        ExpiryDate = DateTime.UtcNow.AddMonths(6),
                        StartDate = DateTime.UtcNow,
                        IsActive = true,
                        PointsRequired = 200,
                        VoucherType = AutoWashPro.DAL.Enums.VoucherType.PhysicalGift,
                        CampaignType = AutoWashPro.DAL.Enums.VoucherCampaignType.Manual,
                        RequiredTierId = firstTier.TierId
                    }
                };
                _context.Vouchers.AddRange(vouchers);
                await _context.SaveChangesAsync();
            }
        }

    }
}
