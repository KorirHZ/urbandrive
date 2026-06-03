using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using UrbanDrive.Models;

namespace UrbanDrive.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Allocation> Allocations { get; set; }
        public DbSet<FuelRecord> FuelRecords { get; set; }
        public DbSet<TripReport> TripReports { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User - Booking relationship (one-to-many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Bookings)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Allocation (as Approver)
            modelBuilder.Entity<User>()
                .HasMany(u => u.AllocationsApproved)
                .WithOne(a => a.Approver)
                .HasForeignKey(a => a.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // User - TripReport (as Submitter)
            modelBuilder.Entity<User>()
                .HasMany(u => u.TripReportsSubmitted)
                .WithOne(t => t.Submitter)
                .HasForeignKey(t => t.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // User - TripReport (as Reviewer)
            modelBuilder.Entity<User>()
                .HasMany(u => u.TripReportsReviewed)
                .WithOne(t => t.Reviewer)
                .HasForeignKey(t => t.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Driver - User (one-to-one)
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking - Allocation (one-to-one)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Allocation)
                .WithOne(a => a.Booking)
                .HasForeignKey<Allocation>(a => a.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Allocation - TripReport (one-to-one)
            modelBuilder.Entity<Allocation>()
                .HasOne(a => a.TripReport)
                .WithOne(t => t.Allocation)
                .HasForeignKey<TripReport>(t => t.AllocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.RegistrationNumber)
                .IsUnique();

            modelBuilder.Entity<Driver>()
                .HasIndex(d => d.LicenseNumber)
                .IsUnique();
        }
    }
}