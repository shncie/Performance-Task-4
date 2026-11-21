using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Data;

public class CareFlowDbContext : DbContext
{
    public CareFlowDbContext(DbContextOptions<CareFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Patient
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.FullName).IsRequired().HasMaxLength(150);
            entity.Property(p => p.ContactNumber).HasMaxLength(50);
        });

        // Configure DiagnosticService
        modelBuilder.Entity<DiagnosticService>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.DailyCapacity).IsRequired();
        });

        // Configure Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.Patient)
                  .WithMany(p => p.Bookings)
                  .HasForeignKey(b => b.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => b.BookingDate);
        });

        // Configure BookingItem
        modelBuilder.Entity<BookingItem>(entity =>
        {
            entity.HasKey(bi => bi.Id);

            entity.HasOne(bi => bi.Booking)
                  .WithMany(b => b.BookingItems)
                  .HasForeignKey(bi => bi.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bi => bi.DiagnosticService)
                  .WithMany(s => s.BookingItems)
                  .HasForeignKey(bi => bi.DiagnosticServiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Database constraint: Prevent duplicate service in the same booking
            entity.HasIndex(bi => new { bi.BookingId, bi.DiagnosticServiceId }).IsUnique();
        });

        // Seed initial fictional Patients
        modelBuilder.Entity<Patient>().HasData(
            new Patient { Id = 1, FullName = "Juan Dela Cruz", DateOfBirth = "1988-04-12", ContactNumber = "+63 917 111 2233" },
            new Patient { Id = 2, FullName = "Maria Santos", DateOfBirth = "1995-09-25", ContactNumber = "+63 920 333 4455" },
            new Patient { Id = 3, FullName = "Antonio Luna", DateOfBirth = "1978-11-03", ContactNumber = "+63 918 555 6677" },
            new Patient { Id = 4, FullName = "Elena Reyes", DateOfBirth = "2001-02-18", ContactNumber = "+63 922 777 8899" },
            new Patient { Id = 5, FullName = "Gabriel Mendoza", DateOfBirth = "1965-07-30", ContactNumber = "+63 915 999 0011" }
        );

        // Seed Diagnostic Services with defined maximum daily capacities
        modelBuilder.Entity<DiagnosticService>().HasData(
            new DiagnosticService { Id = 1, Name = "Complete Blood Count (CBC)", Description = "Routine hematology blood test", DailyCapacity = 5 },
            new DiagnosticService { Id = 2, Name = "Urinalysis", Description = "Routine urine physical/chemical examination", DailyCapacity = 5 },
            new DiagnosticService { Id = 3, Name = "Chest X-Ray", Description = "Diagnostic radiographic chest imaging", DailyCapacity = 3 },
            new DiagnosticService { Id = 4, Name = "ECG (Electrocardiogram)", Description = "Cardiovascular electrical activity test", DailyCapacity = 2 }
        );
    }
}