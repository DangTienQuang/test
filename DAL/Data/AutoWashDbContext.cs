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
        public DbSet<CarModel> CarModels { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Lane> Lanes { get; set; }
        public DbSet<StaffLaneAssignment> StaffLaneAssignments { get; set; }
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }

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

            modelBuilder.Entity<User>()
                .HasOne(u => u.EmployeeProfile)
                .WithOne(e => e.User)
                .HasForeignKey<EmployeeProfile>(e => e.EmployeeId);

            modelBuilder.Entity<DailySlotCapacity>()
                .HasIndex(d => new { d.SlotId, d.Date, d.BranchId })
                .IsUnique();

            modelBuilder.Entity<ServicePrice>()
                .HasOne(sp => sp.Branch)
                .WithMany()
                .HasForeignKey(sp => sp.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TimeSlot>()
                .HasOne(ts => ts.Branch)
                .WithMany()
                .HasForeignKey(ts => ts.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailySlotCapacity>()
                .HasOne(dsc => dsc.Branch)
                .WithMany()
                .HasForeignKey(dsc => dsc.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.ProcessingLane)
                .WithMany(l => l.ProcessingBookings)
                .HasForeignKey(bd => bd.ProcessingLaneId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.ProcessingStaff)
                .WithMany(u => u.ProcessedBookingDetails)
                .HasForeignKey(bd => bd.ProcessingStaffId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Branch)
                .WithMany(br => br.Bookings)
                .HasForeignKey(b => b.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffLaneAssignment>()
                .HasOne(sla => sla.Staff)
                .WithMany(u => u.LaneAssignments)
                .HasForeignKey(sla => sla.StaffId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StaffLaneAssignment>()
                .HasOne(sla => sla.Lane)
                .WithMany(l => l.StaffAssignments)
                .HasForeignKey(sla => sla.LaneId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeProfile>()
                .HasOne(e => e.Branch)
                .WithMany(b => b.Employees)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lane>()
                .HasOne(l => l.Branch)
                .WithMany(b => b.Lanes)
                .HasForeignKey(l => l.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}