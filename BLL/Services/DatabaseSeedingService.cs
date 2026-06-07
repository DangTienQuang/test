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
            SyncCustomerProfilePoints();

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
        }

        private void SyncCustomerProfilePoints()
        {
            const string completionPrefix = "Hoàn thành dịch vụ";
            var now = DateTime.UtcNow;

            var profiles = _context.CustomerProfiles.ToList();
            var allLedgers = _context.PointLedgers.ToList();
            var groupedLedgers = allLedgers.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var profile in profiles)
            {
                if (!groupedLedgers.TryGetValue(profile.UserId, out var ledgers) || !ledgers.Any()) continue;

                var totalAdded = ledgers
                    .Where(p => p.PointsAdded > 0 && (p.ExpiryDate == null || p.ExpiryDate > now))
                    .Sum(p => p.PointsAdded);
                var totalDeducted = ledgers.Where(p => p.PointsDeducted > 0).Sum(p => p.PointsDeducted);
                var promotionFromLedger = ledgers
                    .Where(p => p.PointsAdded > 0 && p.Reason.StartsWith(completionPrefix))
                    .Sum(p => p.PointsAdded);

                if (profile.TotalPoint == 0 && profile.PromotionPoint == 0)
                {
                    profile.TotalPoint = Math.Max(0, totalAdded - totalDeducted);
                    profile.PromotionPoint = promotionFromLedger;
                }
            }

            _context.SaveChanges();
        }
    }
}
