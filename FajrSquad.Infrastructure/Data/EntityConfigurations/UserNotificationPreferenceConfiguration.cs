using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
    {
        public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
        {
            builder.ToTable("UserNotificationPreferences");
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Morning)
                .HasDefaultValue(true);

            builder.Property(x => x.Evening)
                .HasDefaultValue(true);

            builder.Property(x => x.FajrMissed)
                .HasDefaultValue(true);

            builder.Property(x => x.Escalation)
                .HasDefaultValue(true);

            builder.Property(x => x.HadithDaily)
                .HasDefaultValue(true);

            builder.Property(x => x.MotivationDaily)
                .HasDefaultValue(true);

            builder.Property(x => x.EventsNew)
                .HasDefaultValue(true);

            builder.Property(x => x.EventsReminder)
                .HasDefaultValue(true);

            builder.HasOne(x => x.User)
                .WithMany(u => u.UserNotificationPreferences)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserNotificationPreferences_UserId");
        }
    }
}
