using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
    {
        public void Configure(EntityTypeBuilder<NotificationLog> builder)
        {
            builder.ToTable("NotificationLogs");
            builder.HasKey(x => x.Id);


            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PayloadJson)
                .IsRequired()
                .HasDefaultValue("{}");

            builder.Property(x => x.Result)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("Sent");

            builder.Property(x => x.ProviderMessageId)
                .HasMaxLength(200);

            builder.Property(x => x.CollapsibleKey)
                .HasMaxLength(200);

            builder.Property(x => x.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.Retried)
                .HasDefaultValue(0);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_NotificationLogs_UserId");

            builder.HasIndex(x => x.Type)
                .HasDatabaseName("IX_NotificationLogs_Type");

            builder.HasIndex(x => x.SentAt)
                .HasDatabaseName("IX_NotificationLogs_SentAt");

            builder.HasIndex(x => x.Result)
                .HasDatabaseName("IX_NotificationLogs_Result");
        }
    }
}
