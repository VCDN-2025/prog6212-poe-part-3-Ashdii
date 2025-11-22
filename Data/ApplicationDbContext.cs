using CMCSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CMCSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ApprovalLog> ApprovalLogs { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<LecturerRate> LecturerRates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for monetary values
            modelBuilder.Entity<Claim>()
                .Property(c => c.HourlyRate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Claim>()
                .Property(c => c.HoursWorked)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Claim>()
                .Property(c => c.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Lecturer>()
                .Property(l => l.HourlyRate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LecturerRate>()
                .Property(lr => lr.Rate)
                .HasPrecision(18, 2);

            // Seed HR User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "HR Manager",
                    Email = "hr@cmsystem.com",
                    Password = "hr123", 
                    Role = "hr",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}