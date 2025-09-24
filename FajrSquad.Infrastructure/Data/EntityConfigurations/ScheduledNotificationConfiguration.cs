using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class ScheduledNotificationConfiguration : IEntityTypeConfiguration<ScheduledNotification>
    {
        public void Configure(EntityTypeBuilder<ScheduledNotification> builder)
        {
            builder.ToTable("ScheduledNotifications");
            builder.HasKey(x => x.Id);


            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ExecuteAt)
                .IsRequired();

            builder.Property(x => x.DataJson)
                .IsRequired()
                .HasDefaultValue("{}");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            builder.Property(x => x.UniqueKey)
                .HasMaxLength(200);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);

            builder.Property(x => x.Retries)
                .HasDefaultValue(0);

            builder.Property(x => x.MaxRetries)
                .HasDefaultValue(3);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_ScheduledNotifications_UserId");

            builder.HasIndex(x => x.Type)
                .HasDatabaseName("IX_ScheduledNotifications_Type");

            builder.HasIndex(x => x.ExecuteAt)
                .HasDatabaseName("IX_ScheduledNotifications_ExecuteAt");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_ScheduledNotifications_Status");

            builder.HasIndex(x => x.UniqueKey)
                .IsUnique()
                .HasFilter("\"UniqueKey\" IS NOT NULL")
                .HasDatabaseName("IX_ScheduledNotifications_UniqueKey");

            // Optimized indexes for queue processing
            builder.HasIndex(x => new { x.Status, x.ExecuteAt })
                .HasDatabaseName("IX_ScheduledNotifications_Status_ExecuteAt");

            builder.HasIndex(x => new { x.Status, x.NextRetryAt })
                .HasFilter("\"NextRetryAt\" IS NOT NULL")
                .HasDatabaseName("IX_ScheduledNotifications_Status_NextRetryAt");

            builder.HasIndex(x => new { x.UserId, x.Type, x.Status })
                .HasDatabaseName("IX_ScheduledNotifications_UserId_Type_Status");
        }
    }
}
