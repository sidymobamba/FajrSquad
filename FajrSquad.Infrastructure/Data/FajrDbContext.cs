using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Infrastructure.Data
{
    public class FajrDbContext : DbContext
    {
        public FajrDbContext(DbContextOptions<FajrDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<FajrCheckIn> FajrCheckIns => Set<FajrCheckIn>();
        public DbSet<DailyMessage> DailyMessages { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<ProblemReport> ProblemReports { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<FajrCheckIn>().HasKey(f => f.Id);
            modelBuilder.Entity<FajrCheckIn>()
                .HasIndex(f => new { f.UserId, f.Date })
                .IsUnique();
        }
    }
}
