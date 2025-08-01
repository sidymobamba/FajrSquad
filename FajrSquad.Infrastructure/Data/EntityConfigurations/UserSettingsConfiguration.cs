using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.ToTable("UserSettings");
            
            builder.HasKey(us => us.Id);
            
            builder.Property(us => us.UserId)
                .IsRequired();

            // Notification Settings
            builder.Property(us => us.FajrReminder)
                .HasDefaultValue(true);
                
            builder.Property(us => us.MorningHadith)
                .HasDefaultValue(true);
                
            builder.Property(us => us.EveningMotivation)
                .HasDefaultValue(true);
                
            builder.Property(us => us.IslamicHolidays)
                .HasDefaultValue(true);
                
            builder.Property(us => us.FastingReminders)
                .HasDefaultValue(true);
                
            builder.Property(us => us.SleepReminders)
                .HasDefaultValue(true);

            // Timing Settings
            builder.Property(us => us.FajrReminderTime)
                .HasDefaultValue(new TimeSpan(4, 30, 0));
                
            builder.Property(us => us.MorningHadithTime)
                .HasDefaultValue(new TimeSpan(6, 0, 0));
                
            builder.Property(us => us.EveningMotivationTime)
                .HasDefaultValue(new TimeSpan(21, 0, 0));
                
            builder.Property(us => us.SleepReminderTime)
                .HasDefaultValue(new TimeSpan(22, 0, 0));

            // Language & Localization
            builder.Property(us => us.Language)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("fr");
                
            builder.Property(us => us.Timezone)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Europe/Italy");

            // Privacy Settings
            builder.Property(us => us.ShowInLeaderboard)
                .HasDefaultValue(true);
                
            builder.Property(us => us.AllowMotivatingBrotherNotifications)
                .HasDefaultValue(true);
                
            builder.Property(us => us.ShareStreakPublicly)
                .HasDefaultValue(true);

            // App Preferences
            builder.Property(us => us.DarkMode)
                .HasDefaultValue(false);
                
            builder.Property(us => us.SoundEnabled)
                .HasDefaultValue(true);
                
            builder.Property(us => us.VibrationEnabled)
                .HasDefaultValue(true);
                
            builder.Property(us => us.NotificationSound)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("default");
                
            builder.Property(us => us.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(us => us.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(us => us.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserSettings_UserId");

            // Relationships
            builder.HasOne(us => us.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Query filters for soft delete
            builder.HasQueryFilter(us => !us.IsDeleted);
        }
    }
}