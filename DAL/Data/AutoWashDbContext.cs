using AutoWashPro.DAL.Entities;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.DAL.Data
{
    public class AutoWashDbContext : DbContext
    {
        public AutoWashDbContext(DbContextOptions<AutoWashDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<Tier> Tiers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServicePrice> ServicePrices { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<PointLedger> PointLedgers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<UserVoucher> UserVouchers { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<DailySlotCapacity> DailySlotCapacities { get; set; }
        public DbSet<AIConversationLog> AIConversationLogs { get; set; }
        public DbSet<AIKnowledgeBase> AIKnowledgeBases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.CustomerProfile)
                .WithOne(c => c.User)
                .HasForeignKey<CustomerProfile>(c => c.UserId);

            modelBuilder.Entity<DailySlotCapacity>()
                .HasIndex(d => new { d.SlotId, d.Date })
                .IsUnique();

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Service)
                .WithMany()
                .HasForeignKey(bd => bd.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.ActualVehicleType)
                .WithMany()
                .HasForeignKey(bd => bd.ActualVehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}