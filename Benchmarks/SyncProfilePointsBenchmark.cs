using BenchmarkDotNet.Attributes;
using AutoWashPro.DAL.Data;
using Microsoft.EntityFrameworkCore;
using AutoWashPro.DAL.Entities;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class SyncProfilePointsBenchmark
    {
        private AutoWashDbContext _context;

        [Params(10, 100, 500)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AutoWashDbContext>()
                .UseInMemoryDatabase(databaseName: "TestSyncDb")
                .Options;

            _context = new AutoWashDbContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var tier = new Tier { TierName = "T", PointMultiplier = 1, BookingWindowDays = 1, MinAccumulatedPoints = 0 };
            _context.Tiers.Add(tier);
            _context.SaveChanges();

            for (int i = 0; i < N; i++)
            {
                var user = new User { PhoneNumber = $"12345{i}", PasswordHash = "x", Role = "Customer", Status = "Active" };
                _context.Users.Add(user);
            }
            _context.SaveChanges();

            var users = _context.Users.ToList();
            var profiles = new List<CustomerProfile>();
            var ledgers = new List<PointLedger>();

            foreach (var user in users)
            {
                profiles.Add(new CustomerProfile
                {
                    UserId = user.UserId,
                    FullName = $"User {user.UserId}",
                    TierId = tier.TierId,
                    TotalPoint = 0,
                    PromotionPoint = 0
                });

                for (int j = 0; j < 5; j++)
                {
                    ledgers.Add(new PointLedger
                    {
                        UserId = user.UserId,
                        PointsAdded = 10,
                        PointsDeducted = 0,
                        Reason = "Hoàn thành dịch vụ Test",
                        TransactionDate = DateTime.UtcNow
                    });
                }
            }

            _context.CustomerProfiles.AddRange(profiles);
            _context.PointLedgers.AddRange(ledgers);
            _context.SaveChanges();
        }

        [Benchmark(Baseline = true)]
        public void Original()
        {
            const string completionPrefix = "Hoàn thành dịch vụ";
            var now = DateTime.UtcNow;

            foreach (var profile in _context.CustomerProfiles.ToList())
            {
                var ledgers = _context.PointLedgers.Where(p => p.UserId == profile.UserId).ToList();
                if (!ledgers.Any()) continue;

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

            // We do not save changes to avoid accumulating state or we just skip it because EF Core tracking takes time.
        }

        [Benchmark]
        public void Optimized()
        {
            const string completionPrefix = "Hoàn thành dịch vụ";
            var now = DateTime.UtcNow;

            var profiles = _context.CustomerProfiles.ToList();
            var userIds = profiles.Select(p => p.UserId).ToList();

            var allLedgers = _context.PointLedgers
                .Where(p => userIds.Contains(p.UserId))
                .ToList();

            var ledgersByUser = allLedgers.ToLookup(p => p.UserId);

            foreach (var profile in profiles)
            {
                var ledgers = ledgersByUser[profile.UserId];
                if (!ledgers.Any()) continue;

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
        }
    }
}
