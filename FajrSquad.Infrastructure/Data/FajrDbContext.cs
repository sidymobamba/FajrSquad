using System.Net.Mail;
using FajrSquad.Core.Entities;
using FajrSquad.Core.Entities.Adhkar;
using FajrSquad.Infrastructure.Data.EntityConfigurations;
using FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Infrastructure.Data
{
    public class FajrDbContext : DbContext
    {
        public FajrDbContext(DbContextOptions<FajrDbContext> options) : base(options) { }

        // Existing DbSets
        public DbSet<User> Users => Set<User>();
        public DbSet<FajrCheckIn> FajrCheckIns => Set<FajrCheckIn>();
        public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
        public DbSet<DailyMessage> DailyMessages => Set<DailyMessage>();
        public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
        public DbSet<ProblemReport> ProblemReports => Set<ProblemReport>();
        public DbSet<Event> Events { get; set; }
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        // New DbSets - Spiritual Content
        public DbSet<Hadith> Hadiths => Set<Hadith>();
        public DbSet<Motivation> Motivations => Set<Motivation>();
        public DbSet<Reminder> Reminders => Set<Reminder>();

        // New DbSets - User Management
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        
        // New DbSets - Notifications
        public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();
        public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
        public DbSet<ScheduledNotification> ScheduledNotifications => Set<ScheduledNotification>();

        // New DbSets - Adhkar
        public DbSet<Adhkar> Adhkar => Set<Adhkar>();
        public DbSet<AdhkarText> AdhkarTexts => Set<AdhkarText>();
        public DbSet<AdhkarSet> AdhkarSets => Set<AdhkarSet>();
        public DbSet<AdhkarSetItem> AdhkarSetItems => Set<AdhkarSetItem>();
        public DbSet<UserAdhkarProgress> UserAdhkarProgress => Set<UserAdhkarProgress>();
        public DbSet<UserAdhkarBookmark> UserAdhkarBookmarks => Set<UserAdhkarBookmark>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply existing configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new FajrCheckInConfiguration());
            modelBuilder.ApplyConfiguration(new OtpCodeConfiguration());

            // Apply new configurations
            modelBuilder.ApplyConfiguration(new HadithConfiguration());
            modelBuilder.ApplyConfiguration(new MotivationConfiguration());
            modelBuilder.ApplyConfiguration(new ReminderConfiguration());
            modelBuilder.ApplyConfiguration(new UserSettingsConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfiguration());

            // 🔐 NUOVO
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            
            // Notification configurations
            modelBuilder.ApplyConfiguration(new UserNotificationPreferenceConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationLogConfiguration());
            modelBuilder.ApplyConfiguration(new ScheduledNotificationConfiguration());

            // Adhkar configurations
            modelBuilder.ApplyConfiguration(new AdhkarConfiguration());
            modelBuilder.ApplyConfiguration(new AdhkarTextConfiguration());
            modelBuilder.ApplyConfiguration(new AdhkarSetConfiguration());
            modelBuilder.ApplyConfiguration(new AdhkarSetItemConfiguration());
            modelBuilder.ApplyConfiguration(new UserAdhkarProgressConfiguration());
            modelBuilder.ApplyConfiguration(new UserAdhkarBookmarkConfiguration());

            // Additional ad-hoc configs
            ConfigureDailyMessage(modelBuilder);
            ConfigureDeviceToken(modelBuilder);
            ConfigureProblemReport(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void ConfigureDailyMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DailyMessage>(entity =>
            {
                entity.ToTable("DailyMessages");
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Date)
                    .IsRequired()
                    .HasColumnType("date");

                entity.Property(d => d.Message)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasIndex(d => d.Date)
                    .HasDatabaseName("IX_DailyMessages_Date");
            });
        }

        private static void ConfigureDeviceToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceToken>(entity =>
            {
                entity.ToTable("DeviceTokens");
                entity.HasKey(d => d.Id);

                entity.Property(d => d.UserId)
                    .IsRequired();

                entity.Property(d => d.Token)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(d => d.Platform)
                    .HasMaxLength(20)
                    .HasDefaultValue("Android");

                entity.Property(d => d.Language)
                    .HasMaxLength(10)
                    .HasDefaultValue("it");

                entity.Property(d => d.TimeZone)
                    .HasMaxLength(100)
                    .HasDefaultValue("Africa/Dakar");

                entity.Property(d => d.AppVersion)
                    .HasMaxLength(40);

                entity.Property(d => d.IsActive)
                    .HasDefaultValue(true);

                entity.Property(d => d.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(d => d.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(d => new { d.UserId, d.Token })
                    .IsUnique()
                    .HasDatabaseName("IX_DeviceTokens_UserId_Token");

                entity.HasIndex(d => d.UserId)
                    .HasDatabaseName("IX_DeviceTokens_UserId");

                entity.HasIndex(d => d.IsActive)
                    .HasDatabaseName("IX_DeviceTokens_IsActive");

                entity.HasOne(d => d.User)
                    .WithMany(u => u.DeviceTokens)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureProblemReport(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProblemReport>(entity =>
            {
                entity.ToTable("ProblemReports");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.UserId)
                    .IsRequired();

                entity.Property(p => p.Message)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(p => p.UserId)
                    .HasDatabaseName("IX_ProblemReports_UserId");

                entity.HasIndex(p => p.CreatedAt)
                    .HasDatabaseName("IX_ProblemReports_CreatedAt");

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}