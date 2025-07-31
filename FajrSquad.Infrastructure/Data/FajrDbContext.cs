using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Infrastructure.Data
{
    // Quello che dovrebbe essere in FajrSquad.Infrastructure/Data/FajrDbContext.cs
    public class FajrDbContext : DbContext
    {
        public FajrDbContext(DbContextOptions<FajrDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<FajrCheckIn> FajrCheckIns => Set<FajrCheckIn>();
        public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
        public DbSet<DailyMessage> DailyMessages => Set<DailyMessage>();
        public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
        public DbSet<ProblemReport> ProblemReports => Set<ProblemReport>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Applicare tutte le configurazioni
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new FajrCheckInConfiguration());
            modelBuilder.ApplyConfiguration(new OtpCodeConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }

}
