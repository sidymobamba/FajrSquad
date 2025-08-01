using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
    {
        public void Configure(EntityTypeBuilder<Reminder> builder)
        {
            builder.ToTable("Reminders");
            
            builder.HasKey(r => r.Id);
            
            builder.Property(r => r.Title)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(r => r.Message)
                .IsRequired()
                .HasMaxLength(2000);
                
            builder.Property(r => r.Type)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(r => r.Category)
                .HasMaxLength(100);
                
            builder.Property(r => r.ScheduledDate)
                .HasColumnType("date");
                
            builder.Property(r => r.ScheduledTime)
                .HasColumnType("time");
                
            builder.Property(r => r.IsRecurring)
                .HasDefaultValue(false);
                
            builder.Property(r => r.RecurrencePattern)
                .HasMaxLength(50);
                
            builder.Property(r => r.Priority)
                .HasDefaultValue(1);
                
            builder.Property(r => r.IsActive)
                .HasDefaultValue(true);
                
            builder.Property(r => r.Language)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("fr");
                
            builder.Property(r => r.HijriDate)
                .HasMaxLength(100);
                
            builder.Property(r => r.IsHijriCalendar)
                .HasDefaultValue(false);
                
            builder.Property(r => r.AdditionalInfo)
                .HasMaxLength(500);
                
            builder.Property(r => r.ActionUrl)
                .HasMaxLength(200);
                
            builder.Property(r => r.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(r => r.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(r => new { r.Type, r.Language, r.IsActive })
                .HasDatabaseName("IX_Reminders_Type_Language_IsActive");
                
            builder.HasIndex(r => r.ScheduledDate)
                .HasDatabaseName("IX_Reminders_ScheduledDate");
                
            builder.HasIndex(r => r.ScheduledTime)
                .HasDatabaseName("IX_Reminders_ScheduledTime");
                
            builder.HasIndex(r => new { r.IsRecurring, r.RecurrencePattern })
                .HasDatabaseName("IX_Reminders_Recurring");
                
            builder.HasIndex(r => r.Priority)
                .HasDatabaseName("IX_Reminders_Priority");

            // Query filters for soft delete
            builder.HasQueryFilter(r => !r.IsDeleted);
        }
    }
}